/**
 * MbglFrontend.cs — Typed wrapper around mbgl_frontend_t.
 */
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MapLibreNative.Maui;

/// <summary>
/// Wraps <c>mbgl_frontend_t*</c>.
/// <para>
/// <b>Ownership note:</b> once passed to <see cref="MbglMap"/>, the map takes
/// ownership of the underlying native pointer and will destroy it via
/// <c>mbgl_map_destroy</c>. <see cref="Dispose"/> becomes a no-op after
/// <see cref="TransferOwnership"/> is called. Do <em>not</em> call
/// <see cref="Dispose"/> after <see cref="MbglMap.Dispose"/>.
/// </para>
/// </summary>
public sealed class MbglFrontend : IDisposable
{
    internal IntPtr Handle { get; private set; }

    // Set to true after MbglMap takes ownership. Dispose() becomes a no-op
    // but Handle intentionally stays valid so Render/SetSize calls continue
    // to work normally through the frontend's lifetime.
    private bool _ownershipTransferred;

    /// <summary>
    /// Marks the native pointer as owned by the <see cref="MbglMap"/>.
    /// Called automatically by the <see cref="MbglMap"/> constructor.
    /// After this, <see cref="Dispose"/> will not call <c>mbgl_frontend_destroy</c>
    /// (since <c>mbgl_map_destroy</c> already does so), but <see cref="Handle"/>
    /// remains valid for <see cref="Render"/> / <see cref="SetSize"/> calls.
    /// </summary>
    internal void TransferOwnership() => _ownershipTransferred = true;

    private readonly Action _renderCallback;

    // Haelt diese Instanz fuer den nativen Render-Callback erreichbar (userdata)
    private GCHandle _selfHandle;

    // iOS laeuft AOT-only (kein JIT): Delegate-Marshalling native->managed
    // erzeugt den Reverse-Wrapper erst zur Laufzeit und wirft dort eine
    // ExecutionEngineException. Ein statisches UnmanagedCallersOnly-Trampolin
    // mit GCHandle-Userdata ist auf allen Plattformen AOT-sicher.
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void RenderTrampoline(IntPtr userdata)
    {
        try
        {
            if (GCHandle.FromIntPtr(userdata).Target is MbglFrontend frontend)
                frontend._renderCallback();
        }
        catch
        {
            // Exceptions duerfen die native Grenze nie ueberqueren
        }
    }

    /// <param name="surfaceHandle">Platform-specific surface: HDC (Windows), ANativeWindow* (Android), CAMetalLayer* (Apple)</param>
    /// <param name="glContext">WGL context (Windows) or null (Android/Apple)</param>
    /// <param name="widthPx">Initial width in physical pixels</param>
    /// <param name="heightPx">Initial height in physical pixels</param>
    /// <param name="pixelRatio">Device pixel ratio</param>
    /// <param name="onRender">Called by the native layer when a new frame is ready; call <see cref="Render"/> inside it.</param>
    public MbglFrontend(
        IntPtr surfaceHandle,
        IntPtr glContext,
        int    widthPx,
        int    heightPx,
        float  pixelRatio,
        Action onRender)
    {
        _renderCallback = onRender;
        _selfHandle = GCHandle.Alloc(this);

        IntPtr renderFnPtr;
        unsafe
        {
            renderFnPtr = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, void>)&RenderTrampoline;
        }

        Handle = NativeMethods.FrontendCreateGl(
            surfaceHandle, glContext,
            widthPx, heightPx, pixelRatio,
            renderFnPtr, GCHandle.ToIntPtr(_selfHandle));

        if (Handle == IntPtr.Zero)
        {
            _selfHandle.Free();
            throw new InvalidOperationException("mbgl_frontend_create_gl returned null.");
        }
    }

    /// <summary>
    /// Execute the pending render pass. Call from the render thread when
    /// <see cref="onRender"/> fires.
    /// </summary>
    public void Render() => NativeMethods.FrontendRender(Handle);

    public void SetSize(int widthPx, int heightPx)
        => NativeMethods.FrontendSetSize(Handle, widthPx, heightPx);

    /// <summary>
    /// Returns the platform-native view created by the frontend, or <see cref="IntPtr.Zero"/>.
    /// On Apple platforms this is the <c>MTKView*</c>; cast to <c>UIView</c> and add as subview.
    /// </summary>
    public IntPtr GetNativeView() => NativeMethods.FrontendGetNativeView(Handle);

    public void Dispose()
    {
        // Nach mbgl_map_destroy ruft die native Seite den Render-Callback nicht
        // mehr - das Userdata-GCHandle kann unabhaengig von der Ownership-
        // Uebergabe freigegeben werden.
        if (_selfHandle.IsAllocated)
            _selfHandle.Free();

        // If MbglMap took ownership, mbgl_map_destroy already freed this pointer.
        // Do not call mbgl_frontend_destroy — that would be a double-free.
        if (!_ownershipTransferred && Handle != IntPtr.Zero)
        {
            NativeMethods.FrontendDestroy(Handle);
            Handle = IntPtr.Zero;
        }
    }
}
