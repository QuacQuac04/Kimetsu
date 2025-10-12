using Unity.Mathematics;
using System;

/// <summary>
/// _QuadBounds - Ultra-optimized bounds structure for QuadTree
/// Uses float2 for minimal memory footprint and maximum performance
/// </summary>
[Serializable]
public struct _QuadBounds : IEquatable<_QuadBounds>
{
    public float2 min;
    public float2 max;
    
    // Pre-calculated properties to avoid repeated calculations
    public float2 center => (min + max) * 0.5f;
    public float2 size => max - min;
    public float area => (max.x - min.x) * (max.y - min.y);
    
    // Constructor
    public _QuadBounds(float2 min, float2 max)
    {
        this.min = min;
        this.max = max;
    }
    
    public _QuadBounds(float minX, float minY, float maxX, float maxY)
    {
        this.min = new float2(minX, minY);
        this.max = new float2(maxX, maxY);
    }
    
    // Static factory methods for common use cases
    public static _QuadBounds FromCenterAndSize(float2 center, float2 size)
    {
        float2 halfSize = size * 0.5f;
        return new _QuadBounds(center - halfSize, center + halfSize);
    }
    
    public static _QuadBounds FromMinAndSize(float2 min, float2 size)
    {
        return new _QuadBounds(min, min + size);
    }
    
    // Optimized containment checks
    public bool Contains(float2 point)
    {
        return point.x >= min.x && point.x <= max.x &&
               point.y >= min.y && point.y <= max.y;
    }
    
    public bool Contains(_QuadBounds other)
    {
        return other.min.x >= min.x && other.max.x <= max.x &&
               other.min.y >= min.y && other.max.y <= max.y;
    }
    
    // Optimized intersection checks
    public bool Intersects(_QuadBounds other)
    {
        return !(other.min.x > max.x || other.max.x < min.x ||
                 other.min.y > max.y || other.max.y < min.y);
    }
    
    // Fast overlap area calculation
    public float OverlapArea(_QuadBounds other)
    {
        if (!Intersects(other)) return 0f;
        
        float2 overlapMin = math.max(min, other.min);
        float2 overlapMax = math.min(max, other.max);
        float2 overlapSize = overlapMax - overlapMin;
        
        return overlapSize.x * overlapSize.y;
    }
    
    // Expand bounds to include point or bounds
    public _QuadBounds Expand(float2 point)
    {
        return new _QuadBounds(
            math.min(min, point),
            math.max(max, point)
        );
    }
    
    public _QuadBounds Expand(_QuadBounds other)
    {
        return new _QuadBounds(
            math.min(min, other.min),
            math.max(max, other.max)
        );
    }
    
    // Subdivide into 4 quadrants for QuadTree
    public void GetQuadrants(out _QuadBounds nw, out _QuadBounds ne, 
                            out _QuadBounds sw, out _QuadBounds se)
    {
        float2 centerPoint = center;
        
        // Northwest quadrant
        nw = new _QuadBounds(min.x, centerPoint.y, centerPoint.x, max.y);
        
        // Northeast quadrant  
        ne = new _QuadBounds(centerPoint.x, centerPoint.y, max.x, max.y);
        
        // Southwest quadrant
        sw = new _QuadBounds(min.x, min.y, centerPoint.x, centerPoint.y);
        
        // Southeast quadrant
        se = new _QuadBounds(centerPoint.x, min.y, max.x, centerPoint.y);
    }
    
    // Distance calculations for spatial queries
    public float DistanceToPoint(float2 point)
    {
        float2 closest = math.clamp(point, min, max);
        return math.distance(point, closest);
    }
    
    public float SquaredDistanceToPoint(float2 point)
    {
        float2 closest = math.clamp(point, min, max);
        float2 diff = point - closest;
        return math.dot(diff, diff);
    }
    
    // Utility methods
    public bool IsValid()
    {
        return min.x <= max.x && min.y <= max.y;
    }
    
    public _QuadBounds Inflate(float amount)
    {
        return new _QuadBounds(min - amount, max + amount);
    }
    
    public _QuadBounds Scale(float scale)
    {
        float2 centerPoint = center;
        float2 newSize = size * scale;
        float2 halfSize = newSize * 0.5f;
        return new _QuadBounds(centerPoint - halfSize, centerPoint + halfSize);
    }
    
    // IEquatable implementation
    public bool Equals(_QuadBounds other)
    {
        return min.Equals(other.min) && max.Equals(other.max);
    }
    
    public override bool Equals(object obj)
    {
        return obj is _QuadBounds other && Equals(other);
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(min.GetHashCode(), max.GetHashCode());
    }
    
    // Operators
    public static bool operator ==(_QuadBounds left, _QuadBounds right)
    {
        return left.Equals(right);
    }
    
    public static bool operator !=(_QuadBounds left, _QuadBounds right)
    {
        return !left.Equals(right);
    }
    
    // String representation for debugging
    public override string ToString()
    {
        return $"Bounds(min: {min}, max: {max}, size: {size})";
    }
    
    // Static constants for common bounds
    public static readonly _QuadBounds Zero = new _QuadBounds(float2.zero, float2.zero);
    public static readonly _QuadBounds Unit = new _QuadBounds(float2.zero, new float2(1f, 1f));
    public static readonly _QuadBounds Infinite = new _QuadBounds(
        new float2(float.NegativeInfinity, float.NegativeInfinity),
        new float2(float.PositiveInfinity, float.PositiveInfinity)
    );
}
