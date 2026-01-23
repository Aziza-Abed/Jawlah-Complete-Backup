"""
GIS Coordinate Conversion Script
Converts GeoJSON files from Palestine 1923 Grid (EPSG:28191) to WGS84 (EPSG:4326)

This fixes the coordinate system mismatch that breaks geofencing in the FollowUp app.

Usage:
    python convert_to_wgs84.py

Requirements:
    pip install pyproj
"""

import json
from pyproj import Transformer

def convert_coordinates(coords, transformer):
    """
    Recursively convert coordinates from EPSG:28191 to EPSG:4326

    Handles:
    - 2D coordinates: [x, y]
    - 3D coordinates: [x, y, z]
    - Nested arrays of coordinates (for polygons, multipolygons, etc.)
    """
    if not coords:
        return coords

    # Check if this is a coordinate pair/triple (not a nested array)
    if isinstance(coords[0], (int, float)):
        if len(coords) == 2:
            # 2D coordinate: [x, y]
            x, y = coords
            lon, lat = transformer.transform(x, y)
            return [lon, lat]
        elif len(coords) == 3:
            # 3D coordinate: [x, y, z]
            x, y, z = coords
            lon, lat = transformer.transform(x, y)
            return [lon, lat, z]  # Keep z-coordinate as-is
        else:
            return coords
    else:
        # Nested array - recurse deeper
        return [convert_coordinates(item, transformer) for item in coords]

def convert_geojson(input_file, output_file):
    """
    Convert a GeoJSON file from EPSG:28191 to EPSG:4326
    """
    print(f"\n📂 Reading: {input_file}")

    # Read input GeoJSON
    with open(input_file, 'r', encoding='utf-8') as f:
        data = json.load(f)

    # Create coordinate transformer
    # EPSG:28191 = Palestine 1923 / Palestine Grid
    # EPSG:4326 = WGS84 (standard lat/lng)
    transformer = Transformer.from_crs("EPSG:28191", "EPSG:4326", always_xy=True)

    print(f"🔄 Converting coordinates from EPSG:28191 to EPSG:4326...")

    # Convert each feature's geometry
    features_count = len(data.get('features', []))
    for idx, feature in enumerate(data.get('features', []), 1):
        if 'geometry' in feature and feature['geometry']:
            geometry = feature['geometry']
            if 'coordinates' in geometry:
                # Convert coordinates
                geometry['coordinates'] = convert_coordinates(
                    geometry['coordinates'],
                    transformer
                )

        # Show progress every 10 features
        if idx % 10 == 0 or idx == features_count:
            print(f"  Progress: {idx}/{features_count} features converted")

    # Update CRS to WGS84
    data['crs'] = {
        "type": "name",
        "properties": {
            "name": "urn:ogc:def:crs:OGC:1.3:CRS84"
        }
    }

    # Update name if present
    if 'name' in data:
        if '_WGS84' not in data['name']:
            data['name'] = data['name'] + '_WGS84'

    # Write output GeoJSON
    print(f"💾 Writing: {output_file}")
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)

    print(f"✅ Successfully converted {features_count} features!")

    # Show sample coordinates for verification
    if features_count > 0:
        first_feature = data['features'][0]
        if 'geometry' in first_feature and 'coordinates' in first_feature['geometry']:
            coords = first_feature['geometry']['coordinates']
            # Get first coordinate pair
            sample = coords
            while isinstance(sample[0], list):
                sample = sample[0]
            print(f"📍 Sample coordinate: {sample[:2]}")
            print(f"   (Should be ~[35.2, 31.9] for Al-Bireh)")

def main():
    print("=" * 70)
    print("GIS Coordinate Conversion Tool - FollowUp Project")
    print("Converting from Palestine 1923 Grid to WGS84")
    print("=" * 70)

    # Files to convert
    files_to_convert = [
        {
            'input': r'C:\Users\hp\Documents\FollowUp\FollowUp-Repo\GIS\Quarters(Neighborhoods).geojson',
            'output': r'C:\Users\hp\Documents\FollowUp\FollowUp-Repo\GIS\Quarters(Neighborhoods)_WGS84_CORRECT.geojson'
        },
        {
            'input': r'C:\Users\hp\Documents\FollowUp\FollowUp-Repo\GIS\Urban_Master_Plan_Borders_1.geojson',
            'output': r'C:\Users\hp\Documents\FollowUp\FollowUp-Repo\GIS\Urban_Master_Plan_Borders_1_WGS84_CORRECT.geojson'
        }
    ]

    # Convert each file
    for file_pair in files_to_convert:
        try:
            convert_geojson(file_pair['input'], file_pair['output'])
        except FileNotFoundError:
            print(f"❌ ERROR: File not found: {file_pair['input']}")
        except Exception as e:
            print(f"❌ ERROR converting {file_pair['input']}: {e}")
            import traceback
            traceback.print_exc()

    print("\n" + "=" * 70)
    print("✨ Conversion complete!")
    print("=" * 70)
    print("\nNEXT STEPS:")
    print("1. Verify the new files have correct coordinates (~35.2, ~31.9)")
    print("2. Update your backend to use the new *_WGS84_CORRECT.geojson files")
    print("3. Delete or archive the old incorrectly named _WGS84 files")
    print("4. Test geofencing in the mobile app")

if __name__ == '__main__':
    try:
        import pyproj
    except ImportError:
        print("❌ ERROR: pyproj library not installed!")
        print("\nPlease install it first:")
        print("  pip install pyproj")
        print("\nThen run this script again:")
        print("  python convert_to_wgs84.py")
        exit(1)

    main()
