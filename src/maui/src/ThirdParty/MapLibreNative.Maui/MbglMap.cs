/**
 * MbglMap.cs — Typed C# wrapper around the native mbgl_map_t handle.
 *
 * Lifetime: must be disposed on the same thread as its MbglRunLoop.
 * The MbglFrontend must outlive the MbglMap.
 */
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MapLibreNative.Maui;

/// <summary>Wraps <c>mbgl_map_t*</c>. Dispose on the render thread.</summary>
public sealed class MbglMap : IDisposable
{
    internal IntPtr Handle { get; private set; }

    // Haelt den managed Observer fuer das native Callback-Userdata am Leben
    private GCHandle _observerHandle;

    // Kompensation fuer native Binaries OHNE den pixelRatio-Fix (Vendor-README
    // Abweichung 4): Das gefixte C-ABI (Android/Windows) erwartet PHYSISCHE px
    // und teilt intern durch pixelRatio; das Upstream-Binary (aktuell iOS)
    // erwartet direkt LOGISCHE mbgl-px. Mit compatPixelRatio > 1 rechnet dieser
    // Wrapper alle Screen-px-Ein-/Ausgaben an der Grenze um, damit die
    // Controller einheitlich physische px sprechen. AUF 1.0 ZURUECKSETZEN,
    // sobald das iOS-Binary mit dem Fix neu gebaut ist - sonst wird doppelt
    // konvertiert!
    private readonly double _compatPx = 1.0;

    private double L(double physicalPx) => physicalPx / _compatPx;   // physisch -> ABI
    private double P(double abiPx) => abiPx * _compatPx;             // ABI -> physisch

    // iOS-AOT-sicher: statisches UnmanagedCallersOnly-Trampolin statt
    // Delegate-Marshalling (Begruendung siehe MbglFrontend.RenderTrampoline)
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void ObserverTrampoline(IntPtr eventNamePtr, IntPtr detailPtr, IntPtr userdata)
    {
        try
        {
            if (GCHandle.FromIntPtr(userdata).Target is Action<string, string?> observer)
                observer(
                    Marshal.PtrToStringUTF8(eventNamePtr) ?? string.Empty,
                    Marshal.PtrToStringUTF8(detailPtr));
        }
        catch
        {
            // Exceptions duerfen die native Grenze nie ueberqueren
        }
    }

    public MbglMap(
        MbglFrontend frontend,
        MbglRunLoop runLoop,
        string? cachePath = null,
        string? assetPath = null,
        float   pixelRatio = 1.0f,
        Action<string, string?>? observer = null,
        string? apiKey = null,
        ulong   maxCacheSizeBytes = 0,
        float   compatPixelRatio = 1.0f)
    {
        _compatPx = compatPixelRatio > 0 ? compatPixelRatio : 1.0;

        IntPtr observerFnPtr = IntPtr.Zero;
        IntPtr observerUserdata = IntPtr.Zero;
        if (observer != null)
        {
            _observerHandle = GCHandle.Alloc(observer);
            observerUserdata = GCHandle.ToIntPtr(_observerHandle);
            unsafe
            {
                observerFnPtr = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)&ObserverTrampoline;
            }
        }

        Handle = apiKey is null && maxCacheSizeBytes == 0
            ? NativeMethods.MapCreate(
                frontend.Handle, runLoop.Handle,
                cachePath, assetPath,
                pixelRatio,
                observerFnPtr, observerUserdata)
            : NativeMethods.MapCreate2(
                frontend.Handle, runLoop.Handle,
                cachePath, assetPath,
                apiKey, maxCacheSizeBytes,
                pixelRatio,
                observerFnPtr, observerUserdata);

        if (Handle == IntPtr.Zero)
            throw new InvalidOperationException("mbgl_map_create returned null.");

        // mbgl_map_create transfers ownership of the frontend pointer into the
        // native CabiMap struct. Calling mbgl_frontend_destroy afterwards would
        // be a double-free (0xc0000374 heap corruption). Zero the C# handle so
        // MbglFrontend.Dispose() becomes a no-op from this point forward.
        frontend.TransferOwnership();
    }

    public void SetStyleUrl(string url)  => NativeMethods.MapSetStyleUrl(Handle, url);
    public void SetStyleJson(string json) => NativeMethods.MapSetStyleJson(Handle, json);

    public void SetSize(int widthPx, int heightPx)
        => NativeMethods.MapSetSize(Handle,
            Math.Max(1, (int)Math.Round(L(widthPx))),
            Math.Max(1, (int)Math.Round(L(heightPx))));

    public void JumpTo(double lat, double lon, double zoom, double bearing = 0, double pitch = 0)
        => NativeMethods.MapJumpTo(Handle, lat, lon, zoom, bearing, pitch);

    public void EaseTo(double lat, double lon, double zoom, double bearing, double pitch, long durationMs)
        => NativeMethods.MapEaseTo(Handle, lat, lon, zoom, bearing, pitch, durationMs);

    public void FlyTo(double lat, double lon, double zoom, double bearing, double pitch, long durationMs)
        => NativeMethods.MapFlyTo(Handle, lat, lon, zoom, bearing, pitch, durationMs);

    // ── Camera with edge padding ───────────────────────────────────────────────
    // Padding (screen pixels, top/left/bottom/right) shifts the camera's
    // effective centre so the target is centred in the unobscured part of the
    // viewport. Pass double.NaN for zoom/bearing/pitch to keep the current value.

    public void JumpTo(double lat, double lon, double zoom, double bearing, double pitch,
                       double padTop, double padLeft, double padBottom, double padRight)
        => NativeMethods.MapJumpToPadded(Handle, lat, lon, zoom, bearing, pitch,
                                         L(padTop), L(padLeft), L(padBottom), L(padRight));

    public void EaseTo(double lat, double lon, double zoom, double bearing, double pitch,
                       double padTop, double padLeft, double padBottom, double padRight,
                       long durationMs)
        => NativeMethods.MapEaseToPadded(Handle, lat, lon, zoom, bearing, pitch,
                                         L(padTop), L(padLeft), L(padBottom), L(padRight), durationMs);

    public void FlyTo(double lat, double lon, double zoom, double bearing, double pitch,
                      double padTop, double padLeft, double padBottom, double padRight,
                      long durationMs)
        => NativeMethods.MapFlyToPadded(Handle, lat, lon, zoom, bearing, pitch,
                                        L(padTop), L(padLeft), L(padBottom), L(padRight), durationMs);

    /// <summary>Reads the full camera state in one call, optionally offset by edge padding.</summary>
    public CameraResult GetCamera(double padTop = 0, double padLeft = 0,
                                  double padBottom = 0, double padRight = 0)
    {
        NativeMethods.MapGetCamera(Handle, L(padTop), L(padLeft), L(padBottom), L(padRight),
            out var lat, out var lon, out var zoom, out var bearing, out var pitch);
        return new CameraResult(lat, lon, zoom, bearing, pitch);
    }

    /// <summary>Multiply the map scale by <paramref name="scale"/> (2.0 = one zoom level in),
    /// optionally about a screen anchor point (NaN = viewport centre).</summary>
    public void ScaleBy(double scale, double anchorX = double.NaN, double anchorY = double.NaN,
                        long durationMs = 0)
        => NativeMethods.MapScaleBy(Handle, scale, L(anchorX), L(anchorY), durationMs);

    /// <summary>Set geographic constraints and zoom/pitch limits.
    /// Pass <see cref="double.NaN"/> for any parameter to leave it unconstrained.</summary>
    public void SetBounds(double latSw = double.NaN, double lonSw = double.NaN,
                          double latNe = double.NaN, double lonNe = double.NaN,
                          double minZoom = double.NaN, double maxZoom = double.NaN,
                          double minPitch = double.NaN, double maxPitch = double.NaN)
        => NativeMethods.MapSetBounds(Handle, latSw, lonSw, latNe, lonNe,
                                      minZoom, maxZoom, minPitch, maxPitch);

    /// <summary>Returns the CameraOptions (lat, lon, zoom, bearing, pitch) that fits the
    /// given bounds with optional screen padding (top, left, bottom, right in pixels).</summary>
    public (double Lat, double Lon, double Zoom, double Bearing, double Pitch)
        CameraForBounds(double latSw, double lonSw, double latNe, double lonNe,
                        double padTop = 0, double padLeft = 0,
                        double padBottom = 0, double padRight = 0)
    {
        NativeMethods.MapCameraForBounds(Handle, latSw, lonSw, latNe, lonNe,
            L(padTop), L(padLeft), L(padBottom), L(padRight),
            out var lat, out var lon, out var zoom, out var bearing, out var pitch);
        return (lat, lon, zoom, bearing, pitch);
    }

    public (double X, double Y) PixelForLatLng(double lat, double lon)
    {
        NativeMethods.MapPixelForLatLng(Handle, lat, lon, out var x, out var y);
        return (P(x), P(y));
    }

    public (double Lat, double Lon) LatLngForPixel(double x, double y)
    {
        NativeMethods.MapLatLngForPixel(Handle, L(x), L(y), out var lat, out var lon);
        return (lat, lon);
    }

    public void SetProjectionMode(bool axonometric = false, double xSkew = 0.0, double ySkew = 1.0)
        => NativeMethods.MapSetProjectionMode(Handle, axonometric ? 1 : 0, xSkew, ySkew);

    /// <summary>Query rendered features at a screen point. Returns a GeoJSON FeatureCollection string,
    /// or null if the renderer is not ready.</summary>
    /// <param name="layerIds">Optional comma-separated layer IDs to restrict the query.</param>
    public string? QueryRenderedFeaturesAtPoint(double x, double y, string? layerIds = null)
    {
        var ptr = NativeMethods.MapQueryRenderedFeaturesAtPoint(Handle, L(x), L(y), layerIds);
        if (ptr == IntPtr.Zero) return null;
        var result = Marshal.PtrToStringUTF8(ptr);
        NativeMethods.FreeString(ptr);
        return result;
    }

    /// <summary>Query rendered features in a screen bounding box.</summary>
    public string? QueryRenderedFeaturesInBox(double x1, double y1, double x2, double y2,
                                               string? layerIds = null)
    {
        var ptr = NativeMethods.MapQueryRenderedFeaturesInBox(Handle, L(x1), L(y1), L(x2), L(y2), layerIds);
        if (ptr == IntPtr.Zero) return null;
        var result = Marshal.PtrToStringUTF8(ptr);
        NativeMethods.FreeString(ptr);
        return result;
    }

    /// <summary>Query all features in a source's data, regardless of visibility.
    /// Returns a GeoJSON FeatureCollection string, or null if the renderer is not ready.</summary>
    /// <param name="sourceLayerIds">Comma-separated source-layer names — required for
    /// vector sources, ignored for GeoJSON sources.</param>
    /// <param name="filterJson">Optional style-spec filter expression JSON.</param>
    public string? QuerySourceFeatures(string sourceId, string? sourceLayerIds = null,
                                        string? filterJson = null)
    {
        var ptr = NativeMethods.MapQuerySourceFeatures(Handle, sourceId, sourceLayerIds, filterJson);
        if (ptr == IntPtr.Zero) return null;
        var result = Marshal.PtrToStringUTF8(ptr);
        NativeMethods.FreeString(ptr);
        return result;
    }

    /// <summary>Query a feature extension. For clustered GeoJSON sources use extension
    /// <c>"supercluster"</c> with field <c>"children"</c>, <c>"leaves"</c>, or
    /// <c>"expansion-zoom"</c>. Returns JSON (a FeatureCollection or a bare value), or null.</summary>
    public string? QueryFeatureExtensions(string sourceId, string featureJson,
                                           string extension, string extensionField,
                                           string? argsJson = null)
    {
        var ptr = NativeMethods.MapQueryFeatureExtensions(Handle, sourceId, featureJson,
                                                          extension, extensionField, argsJson);
        if (ptr == IntPtr.Zero) return null;
        var result = Marshal.PtrToStringUTF8(ptr);
        NativeMethods.FreeString(ptr);
        return result;
    }

    /// <summary>Returns the zoom level at which the given cluster (a Feature returned by a
    /// rendered-features query on a clustered GeoJSON source) expands, or null.</summary>
    public double? GetClusterExpansionZoom(string sourceId, string clusterFeatureJson)
    {
        var json = QueryFeatureExtensions(sourceId, clusterFeatureJson, "supercluster", "expansion-zoom");
        return json is not null && double.TryParse(json,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var zoom) ? zoom : null;
    }

    /// <summary>Returns the direct children of a cluster as a GeoJSON FeatureCollection string, or null.</summary>
    public string? GetClusterChildren(string sourceId, string clusterFeatureJson)
        => QueryFeatureExtensions(sourceId, clusterFeatureJson, "supercluster", "children");

    /// <summary>Returns up to <paramref name="limit"/> leaf features of a cluster
    /// (from <paramref name="offset"/>) as a GeoJSON FeatureCollection string, or null.</summary>
    public string? GetClusterLeaves(string sourceId, string clusterFeatureJson,
                                     uint limit = 10, uint offset = 0)
        => QueryFeatureExtensions(sourceId, clusterFeatureJson, "supercluster", "leaves",
                                  $"{{\"limit\":{limit},\"offset\":{offset}}}");

    public double Zoom    => NativeMethods.MapGetZoom(Handle);
    public double Bearing => NativeMethods.MapGetBearing(Handle);
    public double Pitch   => NativeMethods.MapGetPitch(Handle);

    public (double Lat, double Lon) Center
    {
        get
        {
            NativeMethods.MapGetCenter(Handle, out var lat, out var lon);
            return (lat, lon);
        }
    }

    public void SetMinZoom(double zoom) => NativeMethods.MapSetMinZoom(Handle, zoom);
    public void SetMaxZoom(double zoom) => NativeMethods.MapSetMaxZoom(Handle, zoom);

    public void OnScroll(double delta, double cx, double cy)
        => NativeMethods.MapOnScroll(Handle, delta, L(cx), L(cy));
    public void OnDoubleTap(double x, double y)
        => NativeMethods.MapOnDoubleTap(Handle, L(x), L(y));
    public void OnPanStart(double x, double y)
        => NativeMethods.MapOnPanStart(Handle, L(x), L(y));
    public void OnPanMove(double dx, double dy)
        => NativeMethods.MapOnPanMove(Handle, L(dx), L(dy));
    public void OnPanEnd()
        => NativeMethods.MapOnPanEnd(Handle);
    public void OnPinch(double scaleFactor, double cx, double cy)
        => NativeMethods.MapOnPinch(Handle, scaleFactor, L(cx), L(cy));

    public void TriggerRepaint() => NativeMethods.MapTriggerRepaint(Handle);
    public void CancelTransitions() => NativeMethods.MapCancelTransitions(Handle);
    public bool IsFullyLoaded => NativeMethods.MapIsFullyLoaded(Handle) != 0;

    // ── Debug overlays ─────────────────────────────────────────────────────────────

    /// <summary>Get the current debug overlay bitmask (<see cref="MbglDebugOptions"/>).</summary>
    public int GetDebugOptions() => NativeMethods.MapGetDebugOptions(Handle);

    /// <summary>Set the debug overlay bitmask. Pass <see cref="MbglDebugOptions.None"/> to disable all.</summary>
    public void SetDebugOptions(int options) => NativeMethods.MapSetDebugOptions(Handle, options);

    // ── Viewport bounds ────────────────────────────────────────────────────────

    public unsafe (double LatSW, double LonSW, double LatNE, double LonNE) LatLngBoundsForCamera()
    {
        double latSW = 0, lonSW = 0, latNE = 0, lonNE = 0;
        NativeMethods.MapLatLngBoundsForCamera(Handle, &latSW, &lonSW, &latNE, &lonNE);
        return (latSW, lonSW, latNE, lonNE);
    }

    // ── Memory / debug ─────────────────────────────────────────────────────────

    public void ReduceMemoryUse() => NativeMethods.MapReduceMemoryUse(Handle);
    public void DumpDebugLogs()   => NativeMethods.MapDumpDebugLogs(Handle);

    // ── Feature state ──────────────────────────────────────────────────────────

    public void SetFeatureState(string sourceId, string featureId, string stateJson,
        string? sourceLayerId = null)
        => NativeMethods.MapSetFeatureState(Handle, sourceId, sourceLayerId, featureId, stateJson);

    public string? GetFeatureState(string sourceId, string featureId,
        string? sourceLayerId = null)
    {
        var ptr = NativeMethods.MapGetFeatureState(Handle, sourceId, sourceLayerId, featureId);
        if (ptr == IntPtr.Zero) return null;
        var result = System.Runtime.InteropServices.Marshal.PtrToStringUTF8(ptr);
        NativeMethods.FreeString(ptr);
        return result;
    }

    public void RemoveFeatureState(string sourceId, string? featureId = null,
        string? stateKey = null, string? sourceLayerId = null)
        => NativeMethods.MapRemoveFeatureState(Handle, sourceId, sourceLayerId, featureId, stateKey);


    public void SetGestureInProgress(bool inProgress)
        => NativeMethods.MapSetGestureInProgress(Handle, inProgress ? 1 : 0);

    public bool IsGestureInProgress => NativeMethods.MapIsGestureInProgress(Handle) != 0;
    /// <summary>True while a rotate transition/animation is running.</summary>
    public bool IsRotating => NativeMethods.MapIsRotating(Handle) != 0;
    /// <summary>True while a zoom/scale transition/animation is running.</summary>
    public bool IsScaling  => NativeMethods.MapIsScaling(Handle) != 0;
    /// <summary>True while a pan transition/animation is running.</summary>
    public bool IsPanning  => NativeMethods.MapIsPanning(Handle) != 0;

    public void MoveBy(double dx, double dy, long durationMs = 0)
        => NativeMethods.MapMoveBy(Handle, L(dx), L(dy), durationMs);

    public void RotateBy(double x0, double y0, double x1, double y1)
        => NativeMethods.MapRotateBy(Handle, L(x0), L(y0), L(x1), L(y1));

    public void PitchBy(double deltaDegrees, long durationMs = 0)
        => NativeMethods.MapPitchBy(Handle, deltaDegrees, durationMs);

    // ── Tier 1 – map option setters ───────────────────────────────────────────
    /// <param name="orientation">0=Upwards 1=Rightwards 2=Downwards 3=Leftwards</param>
    public void SetNorthOrientation(int orientation)
        => NativeMethods.MapSetNorthOrientation(Handle, orientation);

    /// <param name="mode">0=None 1=HeightOnly 2=WidthAndHeight 3=Screen</param>
    public void SetConstrainMode(int mode)
        => NativeMethods.MapSetConstrainMode(Handle, mode);

    /// <param name="mode">0=Default 1=FlippedY</param>
    public void SetViewportMode(int mode)
        => NativeMethods.MapSetViewportMode(Handle, mode);

    // ── Tier 1 – bounds read-back ─────────────────────────────────────────────
    public BoundOptions GetBounds()
    {
        NativeMethods.MapGetBounds(Handle,
            out double latSw, out double lonSw,
            out double latNe, out double lonNe,
            out double minZoom, out double maxZoom,
            out double minPitch, out double maxPitch);
        return new BoundOptions(latSw, lonSw, latNe, lonNe,
                                minZoom, maxZoom, minPitch, maxPitch);
    }

    // ── Tier 2 – prefetch zoom delta ──────────────────────────────────────────
    public void SetPrefetchZoomDelta(int delta)
        => NativeMethods.MapSetPrefetchZoomDelta(Handle, delta);

    public int GetPrefetchZoomDelta()
        => NativeMethods.MapGetPrefetchZoomDelta(Handle);

    // ── Tier 2 – tile LOD controls ────────────────────────────────────────────
    public void SetTileLodMinRadius(double radius)
        => NativeMethods.MapSetTileLodMinRadius(Handle, radius);

    public void SetTileLodScale(double scale)
        => NativeMethods.MapSetTileLodScale(Handle, scale);

    public void SetTileLodPitchThreshold(double thresholdRadians)
        => NativeMethods.MapSetTileLodPitchThreshold(Handle, thresholdRadians);

    public void SetTileLodZoomShift(double shift)
        => NativeMethods.MapSetTileLodZoomShift(Handle, shift);

    /// <param name="mode">0=Default 1=Distance</param>
    public void SetTileLodMode(int mode)
        => NativeMethods.MapSetTileLodMode(Handle, mode);

    // ── Tier 2 – camera for point set ────────────────────────────────────────
    public unsafe CameraResult CameraForLatLngs(
        IReadOnlyList<(double Lat, double Lon)> points,
        double padTop = 0, double padLeft = 0,
        double padBottom = 0, double padRight = 0)
    {
        var flat = new double[points.Count * 2];
        for (int i = 0; i < points.Count; i++)
        {
            flat[i * 2]     = points[i].Lat;
            flat[i * 2 + 1] = points[i].Lon;
        }
        fixed (double* ptr = flat)
        {
            NativeMethods.MapCameraForLatLngs(Handle, ptr, points.Count,
                padTop, padLeft, padBottom, padRight,
                out double lat, out double lon,
                out double zoom, out double bearing, out double pitch);
            return new CameraResult(lat, lon, zoom, bearing, pitch);
        }
    }

    // ── Tier 2 – batch projection ─────────────────────────────────────────────
    public unsafe (double X, double Y)[] PixelsForLatLngs(
        IReadOnlyList<(double Lat, double Lon)> points)
    {
        var flat = new double[points.Count * 2];
        for (int i = 0; i < points.Count; i++)
        {
            flat[i * 2]     = points[i].Lat;
            flat[i * 2 + 1] = points[i].Lon;
        }
        var outXy = new double[points.Count * 2];
        fixed (double* inPtr = flat, outPtr = outXy)
            NativeMethods.MapPixelsForLatLngs(Handle, inPtr, points.Count, outPtr);
        var result = new (double X, double Y)[points.Count];
        for (int i = 0; i < points.Count; i++)
            result[i] = (outXy[i * 2], outXy[i * 2 + 1]);
        return result;
    }

    public unsafe (double Lat, double Lon)[] LatLngsForPixels(
        IReadOnlyList<(double X, double Y)> pixels)
    {
        var flat = new double[pixels.Count * 2];
        for (int i = 0; i < pixels.Count; i++)
        {
            flat[i * 2]     = pixels[i].X;
            flat[i * 2 + 1] = pixels[i].Y;
        }
        var outLl = new double[pixels.Count * 2];
        fixed (double* inPtr = flat, outPtr = outLl)
            NativeMethods.MapLatLngsForPixels(Handle, inPtr, pixels.Count, outPtr);
        var result = new (double Lat, double Lon)[pixels.Count];
        for (int i = 0; i < pixels.Count; i++)
            result[i] = (outLl[i * 2], outLl[i * 2 + 1]);
        return result;
    }

    public MbglStyle GetStyle()
    {
        var styleHandle = NativeMethods.MapGetStyle(Handle);
        if (styleHandle == IntPtr.Zero)
            throw new InvalidOperationException("Style is not yet loaded.");
        return new MbglStyle(styleHandle);
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            NativeMethods.MapDestroy(Handle);
            Handle = IntPtr.Zero;
        }
        if (_observerHandle.IsAllocated)
            _observerHandle.Free();
    }
}

/// <summary>Camera bounds returned by <see cref="MbglMap.GetBounds"/>.</summary>
/// <param name="LatSw">South latitude of the bounding box, or NaN if unset.</param>
/// <param name="LonSw">West longitude of the bounding box, or NaN if unset.</param>
/// <param name="LatNe">North latitude of the bounding box, or NaN if unset.</param>
/// <param name="LonNe">East longitude of the bounding box, or NaN if unset.</param>
/// <param name="MinZoom">Minimum zoom, or NaN if unset.</param>
/// <param name="MaxZoom">Maximum zoom, or NaN if unset.</param>
/// <param name="MinPitch">Minimum pitch (degrees), or NaN if unset.</param>
/// <param name="MaxPitch">Maximum pitch (degrees), or NaN if unset.</param>
public readonly record struct BoundOptions(
    double LatSw, double LonSw,
    double LatNe, double LonNe,
    double MinZoom, double MaxZoom,
    double MinPitch, double MaxPitch);

/// <summary>Camera result from a fit-to-points operation.</summary>
/// <param name="Lat">Center latitude, or NaN if no result.</param>
/// <param name="Lon">Center longitude, or NaN if no result.</param>
/// <param name="Zoom">Zoom level, or NaN if no result.</param>
/// <param name="Bearing">Bearing in degrees, or NaN if no result.</param>
/// <param name="Pitch">Pitch in degrees, or NaN if no result.</param>
public readonly record struct CameraResult(
    double Lat, double Lon,
    double Zoom, double Bearing, double Pitch);
