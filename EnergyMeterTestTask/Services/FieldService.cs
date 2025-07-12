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
using GeoAPI.Geometries;
using GeographicLib;

namespace EnergyMeterTestTask.Services
{
    public class FieldService : IFieldService
    {
        private readonly List<Field> _fields;
        private readonly GeometryFactory _geometryFactory;
        private readonly ICoordinateTransformation _wgs84ToMetric;
        private readonly Geodesic _geodesic;

        public FieldService()
        {
            _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
            _geodesic = Geodesic.WGS84;

            // Setup coordinate transformation (WGS84 to UTM for area calculations)
            var coordinateSystemFactory = new CoordinateSystemFactory();
            var wgs84 = GeographicCoordinateSystem.WGS84;

            // For Moscow region we'll use UTM Zone 37N
            var utm37n = coordinateSystemFactory.CreateFromWkt(
                "PROJCS[\"WGS 84 / UTM zone 37N\"," +
                "GEOGCS[\"WGS 84\"," +
                "DATUM[\"WGS_1984\"," +
                "SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]]," +
                "AUTHORITY[\"EPSG\",\"6326\"]]," +
                "PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]]," +
                "UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]]," +
                "AUTHORITY[\"EPSG\",\"4326\"]]," +
                "UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]]," +
                "PROJECTION[\"Transverse_Mercator\"]," +
                "PARAMETER[\"latitude_of_origin\",0]," +
                "PARAMETER[\"central_meridian\",39]," +
                "PARAMETER[\"scale_factor\",0.9996]," +
                "PARAMETER[\"false_easting\",500000]," +
                "PARAMETER[\"false_northing\",0]," +
                "AUTHORITY[\"EPSG\",\"32637\"]," +
                "AXIS[\"Easting\",EAST]," +
                "AXIS[\"Northing\",NORTH]]");

            var ctFactory = new CoordinateTransformationFactory();
            _wgs84ToMetric = ctFactory.CreateFromCoordinateSystems(wgs84, utm37n);

            _fields = LoadFieldsFromKml("Data/fields.kml", "Data/centroids.kml");
        }

        public IEnumerable<Field> GetAllFields() => _fields;

        public Field GetFieldById(string id) => _fields.FirstOrDefault(f => f.Id == id);

        public double? GetFieldSize(string id) => GetFieldById(id)?.Size;

        public double? CalculateDistanceToCenter(string fieldId, GeoPoint point)
        {
            var field = GetFieldById(fieldId);
            if (field == null) return null;

            // Using GeographicLib for precise geodesic calculations
            return _geodesic.Inverse(
                field.Locations.Center.Latitude, field.Locations.Center.Longitude,
                point.Latitude, point.Longitude).Distance; // in meters
        }

        public (string id, string name)? CheckPointInFields(GeoPoint point)
        {
            var target = _geometryFactory.CreatePoint(
                new NetTopologySuite.Geometries.Coordinate(point.Longitude, point.Latitude));

            foreach (var field in _fields)
            {
                var polygonCoordinates = field.Locations.Polygon
                    .Select(p => new NetTopologySuite.Geometries.Coordinate(p.Longitude, p.Latitude))
                    .Append(new NetTopologySuite.Geometries.Coordinate(field.Locations.Polygon[0].Longitude,
                                         field.Locations.Polygon[0].Latitude))
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
            var culture = CultureInfo.InvariantCulture;

            // Load centroids
            var centroidDict = new Dictionary<string, GeoPoint>();
            var centroidsDoc = XDocument.Load(centroidsPath);

            foreach (var placemark in centroidsDoc.Descendants("{http://www.opengis.net/kml/2.2}Placemark"))
            {
                var id = placemark.Element("{http://www.opengis.net/kml/2.2}name")?.Value;
                var coords = placemark.Descendants("{http://www.opengis.net/kml/2.2}coordinates")
                                    .First().Value.Trim().Split(',');

                if (id != null && coords.Length >= 2 &&
                    double.TryParse(coords[1], NumberStyles.Any, culture, out var lat) &&
                    double.TryParse(coords[0], NumberStyles.Any, culture, out var lng))
                {
                    centroidDict[id] = new GeoPoint(lat, lng);
                }
            }

            // Load fields
            var fieldsDoc = XDocument.Load(fieldsPath);

            foreach (var placemark in fieldsDoc.Descendants("{http://www.opengis.net/kml/2.2}Placemark"))
            {
                var id = placemark.Element("{http://www.opengis.net/kml/2.2}name")?.Value;
                var description = placemark.Element("{http://www.opengis.net/kml/2.2}description")?.Value;

                if (id == null || !centroidDict.TryGetValue(id, out var center)) continue;

                var coordsText = placemark.Descendants("{http://www.opengis.net/kml/2.2}coordinates")
                                        .First().Value;

                var polygonPoints = new List<GeoPoint>();
                var coordPairs = coordsText.Split(new[] { ' ', '\n', '\r', '\t' },
                                                StringSplitOptions.RemoveEmptyEntries);

                foreach (var pair in coordPairs)
                {
                    var parts = pair.Split(',');
                    if (parts.Length < 2) continue;

                    if (double.TryParse(parts[1], NumberStyles.Any, culture, out var lat) &&
                        double.TryParse(parts[0], NumberStyles.Any, culture, out var lng))
                    {
                        polygonPoints.Add(new GeoPoint(lat, lng));
                    }
                }

                if (polygonPoints.Count < 3) continue;

                // Calculate area using projected coordinates
                var area = CalculatePolygonArea(polygonPoints);

                fields.Add(new Field
                {
                    Id = id,
                    Name = description ?? $"Field {id}",
                    Size = Math.Round(area / 10000, 2), // Convert to hectares
                    Locations = new FieldLocation
                    {
                        Center = center,
                        Polygon = polygonPoints
                    }
                });
            }

            return fields;
        }

        private double CalculatePolygonArea(List<GeoPoint> points)
        {
            // Transform coordinates to metric system
            var metricCoords = points
                .Select(p => _wgs84ToMetric.MathTransform.Transform(
                    new[] { p.Longitude, p.Latitude }))
                .Select(c => new NetTopologySuite.Geometries.Coordinate(c[0], c[1]))
                .ToList();

            // Close the ring
            metricCoords.Add(metricCoords[0]);

            var polygon = _geometryFactory.CreatePolygon(metricCoords.ToArray());
            return polygon.Area; // in square meters
        }
    }
}
