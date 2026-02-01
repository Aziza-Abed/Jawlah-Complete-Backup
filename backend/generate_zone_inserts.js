const fs = require('fs');

// Read the GeoJSON file
const data = JSON.parse(fs.readFileSync('C:\\Users\\hp\\Documents\\FollowUp\\Jawlah-Repo\\GIS\\Quarters(Neighborhoods)_WGS84_CORRECT.geojson', 'utf8'));

console.log("-- ========================================================================");
console.log("-- 3. ZONES (Real Al-Bireh Neighborhoods from GIS data)");
console.log("-- ========================================================================");
console.log("PRINT '3. Creating Zones...'\n");

const zoneVariables = [];

data.features.forEach((feature, idx) => {
    const props = feature.properties;
    const geom = feature.geometry;
    const featureIdx = idx + 1;

    // Extract properties
    const quarterNum = props.Quarter_Nu || '';
    const quarterNameAr = props.QuarterNam || '';
    const quarterNameEn = props.QuarterN_1 || '';
    const area = props.SHAPE_Area || 0;

    // Calculate center point (simple average of coordinates)
    let firstRing;
    if (geom.type === 'MultiPolygon') {
        firstRing = geom.coordinates[0][0];
    } else { // Polygon
        firstRing = geom.coordinates[0];
    }

    // Calculate centroid (average of all points)
    const lats = firstRing.map(point => point[1]);
    const lons = firstRing.map(point => point[0]);
    const centerLat = lats.reduce((a, b) => a + b, 0) / lats.length;
    const centerLon = lons.reduce((a, b) => a + b, 0) / lons.length;

    // Generate zone code
    const zoneCode = quarterNum ? `ZONE${quarterNum.padStart(2, '0')}` : `ZONE${featureIdx.toString().padStart(2, '0')}`;

    // Convert geometry to GeoJSON string (escape single quotes)
    const geomJson = JSON.stringify(geom).replace(/'/g, "''");

    // Generate variable name - clean up special characters
    const cleanNum = quarterNum.replace(/[()]/g, '').replace(/ /g, '');
    const varName = quarterNum ? `@Zone${cleanNum}` : `@Zone${featureIdx}`;
    zoneVariables.push(varName);

    // Generate INSERT statement (BoundaryGeoJson stored separately via GIS import)
    console.log(`-- Zone ${featureIdx}: ${quarterNameAr} (${quarterNameEn})`);
    console.log(`INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, Version, VersionDate, IsActive, CreatedAt)`);
    console.log(`VALUES (@MunicipalityId, N'${quarterNameAr}', '${zoneCode}', N'${quarterNameEn}', ${centerLat.toFixed(6)}, ${centerLon.toFixed(6)}, ${area.toFixed(2)}, N'البيرة', 1, GETUTCDATE(), 1, GETUTCDATE());`);
    console.log(`DECLARE ${varName} INT = SCOPE_IDENTITY();`);
    console.log();
});

console.log(`\nPRINT 'Zones created: ${data.features.length} zones (real Al-Bireh neighborhoods)';\n`);

// Generate zone array for easy random selection
console.log("-- Create array of all zone IDs for random task assignment");
console.log("DECLARE @AllZones TABLE (ZoneId INT);");
console.log("INSERT INTO @AllZones VALUES");
zoneVariables.forEach((varName, idx) => {
    const comma = idx < zoneVariables.length - 1 ? ',' : ';';
    console.log(`(${varName})${comma}`);
});
console.log();
