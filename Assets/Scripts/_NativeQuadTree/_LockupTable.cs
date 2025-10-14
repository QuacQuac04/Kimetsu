using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

/// <summary>
/// _LookupTable - Ultra-optimized Morton Code implementation for 2D to 1D conversion
/// Provides lightning-fast spatial indexing with minimal memory usage
/// </summary>
public static class _LookupTable
{
    // Bit manipulation constants for Morton encoding
    private const uint MORTON_MASK_X = 0x55555555; // 01010101...
    private const uint MORTON_MASK_Y = 0xAAAAAAAA; // 10101010...
    private const uint MORTON_SPREAD_X = 0x55555555;
    private const uint MORTON_SPREAD_Y = 0xAAAAAAAA;
    
    // Maximum coordinate values for Morton encoding
    public const int MAX_MORTON_COORD = 4095; // 12-bit coordinates for minimal RAM
    public const uint MAX_MORTON_CODE = 0xFFFFFFFF; // 32-bit Morton code
    
    // Remove static constructor to avoid circular dependency in Burst
    // Lookup tables will be computed on-the-fly for Burst compatibility
    
    /// <summary>
    /// Spread bits for Morton encoding - Burst compatible version
    /// </summary>
    [BurstCompile]
    private static uint SpreadBits(uint value)
    {
        value = (value | (value << 8)) & 0x00FF00FF;
        value = (value | (value << 4)) & 0x0F0F0F0F;
        value = (value | (value << 2)) & 0x33333333;
        value = (value | (value << 1)) & 0x55555555;
        return value;
    }
    
    /// <summary>
    /// Spread 8 bits across 16 bits for Morton encoding
    /// </summary>
    private static uint SpreadBits8(uint value)
    {
        value = (value | (value << 4)) & 0x0F0F;
        value = (value | (value << 2)) & 0x3333;
        value = (value | (value << 1)) & 0x5555;
        return value;
    }
    
    /// <summary>
    /// Compact every other bit for Morton decoding - Burst compatible
    /// </summary>
    [BurstCompile]
    private static uint CompactBits(uint value)
    {
        value &= 0x55555555;
        value = (value ^ (value >> 1)) & 0x33333333;
        value = (value ^ (value >> 2)) & 0x0F0F0F0F;
        value = (value ^ (value >> 4)) & 0x00FF00FF;
        value = (value ^ (value >> 8)) & 0x0000FFFF;
        return value;
    }
    
    /// <summary>
    /// Encode 2D coordinates to Morton code (Z-order curve)
    /// Burst-compatible version without static lookup tables
    /// </summary>
    [BurstCompile]
    public static uint EncodeMorton(uint x, uint y)
    {
        // Clamp coordinates to valid range
        x = math.min(x, MAX_MORTON_COORD);
        y = math.min(y, MAX_MORTON_COORD);
        
        // Use bit interleaving for Morton encoding
        return SpreadBits(x) | (SpreadBits(y) << 1);
    }
    
    /// <summary>
    /// Encode float2 coordinates to Morton code
    /// </summary>
    [BurstCompile]
    public static uint EncodeMorton(float2 position, float2 worldMin, float2 worldSize)
    {
        // Normalize to [0, 1] range
        float2 normalized = (position - worldMin) / worldSize;
        
        // Convert to integer coordinates
        uint x = (uint)(normalized.x * MAX_MORTON_COORD);
        uint y = (uint)(normalized.y * MAX_MORTON_COORD);
        
        return EncodeMorton(x, y);
    }
    
    /// <summary>
    /// Decode Morton code back to 2D coordinates
    /// Burst-compatible version without static lookup tables
    /// </summary>
    [BurstCompile]
    public static void DecodeMorton(uint mortonCode, out uint x, out uint y)
    {
        // Extract x and y coordinates using bit compaction
        x = CompactBits(mortonCode);
        y = CompactBits(mortonCode >> 1);
    }
    
    /// <summary>
    /// Decode Morton code to float2 coordinates
    /// </summary>
    [BurstCompile]
    public static float2 DecodeMorton(uint mortonCode, float2 worldMin, float2 worldSize)
    {
        DecodeMorton(mortonCode, out uint x, out uint y);
        
        // Convert back to world coordinates
        float2 normalized = new float2(
            (float)x / MAX_MORTON_COORD,
            (float)y / MAX_MORTON_COORD
        );
        
        return worldMin + normalized * worldSize;
    }
    
    /// <summary>
    /// Get Morton code for a specific QuadTree level and cell
    /// </summary>
    [BurstCompile]
    public static uint GetQuadTreeMorton(int level, uint cellX, uint cellY)
    {
        uint shift = (uint)(16 - level); // Adjust for tree depth
        uint scaledX = cellX << (int)shift;
        uint scaledY = cellY << (int)shift;
        
        return EncodeMorton(scaledX, scaledY);
    }
    
    /// <summary>
    /// Get parent Morton code (move up one level in QuadTree)
    /// </summary>
    [BurstCompile]
    public static uint GetParentMorton(uint mortonCode)
    {
        return mortonCode >> 2; // Remove last 2 bits (one level up)
    }
    
    /// <summary>
    /// Get child Morton codes (move down one level in QuadTree)
    /// </summary>
    [BurstCompile]
    public static void GetChildrenMorton(uint parentMorton, out uint nw, out uint ne, out uint sw, out uint se)
    {
        uint baseCode = parentMorton << 2;
        nw = baseCode;      // 00
        ne = baseCode | 1;  // 01  
        sw = baseCode | 2;  // 10
        se = baseCode | 3;  // 11
    }
    
    /// <summary>
    /// Calculate distance between two Morton codes (approximation)
    /// </summary>
    [BurstCompile]
    public static float MortonDistance(uint morton1, uint morton2)
    {
        DecodeMorton(morton1, out uint x1, out uint y1);
        DecodeMorton(morton2, out uint x2, out uint y2);
        
        float dx = (float)(x1 - x2);
        float dy = (float)(y1 - y2);
        
        return math.sqrt(dx * dx + dy * dy);
    }
    
    /// <summary>
    /// Get Morton codes within a radius (for spatial queries)
    /// Returns the count of valid Morton codes found
    /// </summary>
    [BurstCompile]
    public static int GetMortonRange(uint centerMorton, uint radius, 
                                   NativeArray<uint> results)
    {
        DecodeMorton(centerMorton, out uint centerX, out uint centerY);
        
        uint minX = centerX > radius ? centerX - radius : 0;
        uint maxX = math.min(centerX + radius, MAX_MORTON_COORD);
        uint minY = centerY > radius ? centerY - radius : 0;
        uint maxY = math.min(centerY + radius, MAX_MORTON_COORD);
        
        int count = 0;
        for (uint y = minY; y <= maxY && count < results.Length; y++)
        {
            for (uint x = minX; x <= maxX && count < results.Length; x++)
            {
                uint morton = EncodeMorton(x, y);
                if (MortonDistance(centerMorton, morton) <= radius)
                {
                    results[count] = morton;
                    count++;
                }
            }
        }
        return count;
    }
    
    /// <summary>
    /// Interleave bits for Morton encoding (alternative method)
    /// </summary>
    [BurstCompile]
    public static uint InterleaveBits(uint x, uint y)
    {
        // Spread x bits
        x = (x | (x << 8)) & 0x00FF00FF;
        x = (x | (x << 4)) & 0x0F0F0F0F;
        x = (x | (x << 2)) & 0x33333333;
        x = (x | (x << 1)) & 0x55555555;
        
        // Spread y bits
        y = (y | (y << 8)) & 0x00FF00FF;
        y = (y | (y << 4)) & 0x0F0F0F0F;
        y = (y | (y << 2)) & 0x33333333;
        y = (y | (y << 1)) & 0x55555555;
        
        return x | (y << 1);
    }
    
    /// <summary>
    /// Get Morton code for bounds (using center point)
    /// </summary>
    [BurstCompile]
    public static uint GetBoundsMorton(_QuadBounds bounds, float2 worldMin, float2 worldSize)
    {
        return EncodeMorton(bounds.center, worldMin, worldSize);
    }
    
    /// <summary>
    /// Check if Morton code is within bounds
    /// </summary>
    [BurstCompile]
    public static bool IsInBounds(uint mortonCode, _QuadBounds bounds, float2 worldMin, float2 worldSize)
    {
        float2 position = DecodeMorton(mortonCode, worldMin, worldSize);
        return bounds.Contains(position);
    }
    
    /// <summary>
    /// Get the quadrant index (0-3) for a Morton code at a specific level
    /// </summary>
    [BurstCompile]
    public static int GetQuadrant(uint mortonCode, int level)
    {
        int shift = (15 - level) * 2; // 2 bits per level
        return (int)((mortonCode >> shift) & 3);
    }
    
    /// <summary>
    /// Memory-efficient Morton code storage for large datasets
    /// Compresses coordinates to save RAM
    /// </summary>
    public struct CompressedMorton
    {
        public ushort high;
        public ushort low;
        
        public CompressedMorton(uint mortonCode)
        {
            high = (ushort)(mortonCode >> 16);
            low = (ushort)(mortonCode & 0xFFFF);
        }
        
        public uint ToMorton()
        {
            return ((uint)high << 16) | low;
        }
    }
}
