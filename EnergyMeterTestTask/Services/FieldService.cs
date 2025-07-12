using System.Globalization;
using System.Xml.Linq;
using EnergyMeterTestTask.Models;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using NetTopologySuite.IO.KML;
using GeoAPI.CoordinateSystems.Transformations;
using ProjNet;

namespace EnergyMeterTestTask.Services
{
    public class FieldService : IFieldService
    {
        private readonly List<Field> _fields;
        private readonly GeometryFactory _geometryFactory;

        public FieldService()
        {
            _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
            _fields = LoadFieldsFromKml("Data/fields.kml", "Data/centroids.kml");
        }

        public IEnumerable<Field> GetAllFields() => _fields;

        public Field GetFieldById(string id) => _fields.FirstOrDefault(f => f.Id == id);

        public double? GetFieldSize(string id) => GetFieldById(id)?.Size;

        public double? CalculateDistanceToCenter(string fieldId, Models.Point point)
        {
            var field = GetFieldById(fieldId);
            if (field == null) return null;

            var center = _geometryFactory.CreatePoint(new Coordinate(field.Locations.Center.Lng, field.Locations.Center.Lat));
            var target = _geometryFactory.CreatePoint(new Coordinate(point.Lng, point.Lat));

            // Convert to metric system (using Haversine formula for WGS84)
            return center.Distance(target) * 111320; // Approximate conversion to meters
        }

        public (string id, string name)? CheckPointInFields(Models.Point point)
        {
            var target = _geometryFactory.CreatePoint(new Coordinate(point.Lng, point.Lat));

            foreach (var field in _fields)
            {
                var polygonCoordinates = field.Locations.Polygon
                    .Select(p => new Coordinate(p.Lng, p.Lat))
                    .Append(new Coordinate(field.Locations.Polygon[0].Lng, field.Locations.Polygon[0].Lat)) // Close the ring
                    .ToArray();

                var polygon = _geometryFactory.CreatePolygon(polygonCoordinates);

                if (polygon.Contains(target))
                {
                    return (field.Id, field.Name);
                }
            }

            return null;
        }

        private List<Field> LoadFieldsFromKml(string fieldsPath, string centroidsPath)
        {
            var fields = new List<Field>();

            // Используем инвариантную культуру для парсинга чисел
            var culture = CultureInfo.InvariantCulture;

            // Load centroids first
            var centroidsDoc = XDocument.Load(centroidsPath);
            var centroidPlacemarks = centroidsDoc.Descendants("{http://www.opengis.net/kml/2.2}Placemark");

            var centroidDict = new Dictionary<string, Models.Point>();
            foreach (var placemark in centroidPlacemarks)
            {
                var id = placemark.Element("{http://www.opengis.net/kml/2.2}name")?.Value;
                var coordinates = placemark.Descendants("{http://www.opengis.net/kml/2.2}coordinates").First().Value;
                var coords = coordinates.Trim().Split(',');

                if (id != null && coords.Length >= 2)
                {
                    centroidDict[id] = new  Models.Point(
                        double.Parse(coords[1], culture),
                        double.Parse(coords[0], culture));
                }
            }

            // Load fields polygons
            var fieldsDoc = XDocument.Load(fieldsPath);
            var fieldPlacemarks = fieldsDoc.Descendants("{http://www.opengis.net/kml/2.2}Placemark");

            foreach (var placemark in fieldPlacemarks)
            {
                var id = placemark.Element("{http://www.opengis.net/kml/2.2}name")?.Value;
                var description = placemark.Element("{http://www.opengis.net/kml/2.2}description")?.Value;
                var coordinates = placemark.Descendants("{http://www.opengis.net/kml/2.2}coordinates").First().Value;

                if (id != null && centroidDict.TryGetValue(id, out var center))
                {
                    var polygonPoints = coordinates.Trim()
                        .Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(coord => coord.Split(','))
                        .Where(parts => parts.Length >= 2)
                        .Select(parts => new Models.Point(
                            double.Parse(parts[1], culture),
                            double.Parse(parts[0], culture)))
                        .ToList();

                    // Calculate area
                    var polygon = _geometryFactory.CreatePolygon(
                        polygonPoints
                            .Select(p => new Coordinate(p.Lng, p.Lat))
                            .Append(new Coordinate(polygonPoints[0].Lng, polygonPoints[0].Lat))
                            .ToArray());

                    var area = polygon.Area * 111 * 111 * 100; // in hectares

                    fields.Add(new Field
                    {
                        Id = id,
                        Name = description ?? $"Field {id}",
                        Size = Math.Round(area, 2),
                        Locations = new FieldLocation
                        {
                            Center = center,
                            Polygon = polygonPoints
                        }
                    });
                }
            }

            return fields;
        }
    }
}
