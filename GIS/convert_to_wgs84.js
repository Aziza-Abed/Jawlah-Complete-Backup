/**
 * GIS Coordinate Conversion Script (Node.js version)
 * Converts GeoJSON files from Palestine 1923 Grid (EPSG:28191) to WGS84 (EPSG:4326)
 *
 * Usage: node convert_to_wgs84.js
 *
 * Requirements: npm install proj4
 */

const fs = require('fs');
const path = require('path');

// Import proj4 - will be installed via npm
let proj4;
try {
    proj4 = require('proj4');
} catch (e) {
    console.error('❌ ERROR: proj4 library not installed!');
    console.error('\nPlease install it first:');
    console.error('  npm install proj4');
    console.error('\nThen run this script again:');
    console.error('  node convert_to_wgs84.js');
    process.exit(1);
}

// Define coordinate systems
// EPSG:28191 - Palestine 1923 / Palestine Grid
proj4.defs("EPSG:28191", "+proj=cass +lat_0=31.73409694444445 +lon_0=35.21208055555556 +x_0=170251.555 +y_0=126867.909 +a=6378300.789 +b=6378300.789 +units=m +no_defs");

// EPSG:4326 - WGS84 (standard lat/lng)
proj4.defs("EPSG:4326", "+proj=longlat +datum=WGS84 +no_defs");

// Create transformer
const transformer = proj4("EPSG:28191", "EPSG:4326");

/**
 * Recursively convert coordinates from EPSG:28191 to EPSG:4326
 */
function convertCoordinates(coords) {
    if (!coords || coords.length === 0) {
        return coords;
    }

    // Check if this is a coordinate pair/triple (not a nested array)
    if (typeof coords[0] === 'number') {
        if (coords.length === 2) {
            // 2D coordinate: [x, y]
            const [lon, lat] = transformer.forward([coords[0], coords[1]]);
            return [lon, lat];
        } else if (coords.length === 3) {
            // 3D coordinate: [x, y, z]
            const [lon, lat] = transformer.forward([coords[0], coords[1]]);
            return [lon, lat, coords[2]]; // Keep z-coordinate as-is
        } else {
            return coords;
        }
    } else {
        // Nested array - recurse deeper
        return coords.map(item => convertCoordinates(item));
    }
}

/**
 * Convert a GeoJSON file from EPSG:28191 to EPSG:4326
 */
function convertGeoJSON(inputFile, outputFile) {
    console.log(`\n📂 Reading: ${path.basename(inputFile)}`);

    // Read input GeoJSON
    const data = JSON.parse(fs.readFileSync(inputFile, 'utf8'));

    console.log(`🔄 Converting coordinates from EPSG:28191 to EPSG:4326...`);

    // Convert each feature's geometry
    const features = data.features || [];
    const featuresCount = features.length;

    features.forEach((feature, idx) => {
        if (feature.geometry && feature.geometry.coordinates) {
            // Convert coordinates
            feature.geometry.coordinates = convertCoordinates(
                feature.geometry.coordinates
            );
        }

        // Show progress every 10 features
        if ((idx + 1) % 10 === 0 || (idx + 1) === featuresCount) {
            console.log(`  Progress: ${idx + 1}/${featuresCount} features converted`);
        }
    });

    // Update CRS to WGS84
    data.crs = {
        "type": "name",
        "properties": {
            "name": "urn:ogc:def:crs:OGC:1.3:CRS84"
        }
    };

    // Update name if present
    if (data.name && !data.name.includes('_WGS84')) {
        data.name = data.name + '_WGS84';
    }

    // Write output GeoJSON
    console.log(`💾 Writing: ${path.basename(outputFile)}`);
    fs.writeFileSync(outputFile, JSON.stringify(data, null, 2), 'utf8');

    console.log(`✅ Successfully converted ${featuresCount} features!`);

    // Show sample coordinates for verification
    if (featuresCount > 0) {
        const firstFeature = features[0];
        if (firstFeature.geometry && firstFeature.geometry.coordinates) {
            let sample = firstFeature.geometry.coordinates;
            // Get first coordinate pair
            while (Array.isArray(sample[0]) && Array.isArray(sample[0][0])) {
                sample = sample[0];
            }
            if (Array.isArray(sample[0])) {
                sample = sample[0];
            }
            console.log(`📍 Sample coordinate: [${sample[0].toFixed(6)}, ${sample[1].toFixed(6)}]`);
            console.log(`   (Should be ~[35.2, 31.9] for Al-Bireh)`);

            // Verify coordinates are in valid WGS84 range
            const lon = sample[0];
            const lat = sample[1];
            if (lon >= 34 && lon <= 36 && lat >= 31 && lat <= 33) {
                console.log(`   ✅ Coordinates look correct for Palestine region!`);
            } else {
                console.log(`   ⚠️  WARNING: Coordinates outside expected range!`);
            }
        }
    }
}

function main() {
    console.log("=".repeat(70));
    console.log("GIS Coordinate Conversion Tool - FollowUp Project");
    console.log("Converting from Palestine 1923 Grid to WGS84");
    console.log("=".repeat(70));

    // Files to convert
    // NOTE: These files are incorrectly named _WGS84 but contain EPSG:28191 projected coordinates
    const filesToConvert = [
        {
            input: path.join(__dirname, 'Quarters(Neighborhoods)_WGS84.geojson'),
            output: path.join(__dirname, 'Quarters(Neighborhoods)_WGS84_CORRECT.geojson')
        },
        {
            input: path.join(__dirname, 'Urban_Master_Plan_Borders_1_WGS84.geojson'),
            output: path.join(__dirname, 'Urban_Master_Plan_Borders_1_WGS84_CORRECT.geojson')
        }
    ];

    // Convert each file
    let successCount = 0;
    for (const filePair of filesToConvert) {
        try {
            convertGeoJSON(filePair.input, filePair.output);
            successCount++;
        } catch (error) {
            if (error.code === 'ENOENT') {
                console.log(`❌ ERROR: File not found: ${path.basename(filePair.input)}`);
            } else {
                console.log(`❌ ERROR converting ${path.basename(filePair.input)}: ${error.message}`);
                console.error(error.stack);
            }
        }
    }

    console.log("\n" + "=".repeat(70));
    console.log(`✨ Conversion complete! (${successCount}/${filesToConvert.length} files converted)`);
    console.log("=".repeat(70));

    if (successCount > 0) {
        console.log("\n📋 NEXT STEPS:");
        console.log("1. ✅ Verify the new files have correct coordinates (~35.2, ~31.9)");
        console.log("2. Update your backend to use the new *_WGS84_CORRECT.geojson files");
        console.log("3. Delete or archive the old incorrectly named _WGS84 files");
        console.log("4. Test geofencing in the mobile app");
    }
}

// Run the conversion
main();
