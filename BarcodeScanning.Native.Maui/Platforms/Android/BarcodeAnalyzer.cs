﻿using Android.Gms.Extensions;
using AndroidX.Camera.Core;
using AndroidX.Camera.View;
using AndroidX.Camera.View.Transform;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Common;

using Size = Android.Util.Size;

namespace BarcodeScanning.Platforms.Android;

internal class BarcodeAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
{
    public Size DefaultTargetResolution => Methods.TargetResolution(_cameraView.CaptureQuality);
    public int TargetCoordinateSystem => ImageAnalysis.CoordinateSystemOriginal;

    private readonly IBarcodeScanner _barcodeScanner;
    private readonly CameraView _cameraView;
    private readonly PreviewView _previewView;

    internal BarcodeAnalyzer(CameraView cameraView, PreviewView previewView)
    {
        _cameraView = cameraView;
        _previewView = previewView;

        _barcodeScanner = Xamarin.Google.MLKit.Vision.BarCode.BarcodeScanning.GetClient(new BarcodeScannerOptions.Builder()
            .SetBarcodeFormats(Methods.ConvertBarcodeFormats(_cameraView.BarcodeSymbologies))
            .Build());
    }

    public async void Analyze(IImageProxy proxy)
    {
        try
        {
            if (proxy is null || proxy.Image is null || _cameraView.PauseScanning)
                return;

            var target = await MainThread.InvokeOnMainThreadAsync(() => _previewView.OutputTransform);
            var source = new ImageProxyTransformFactory
            {
                UsingRotationDegrees = true
            }
            .GetOutputTransform(proxy);
            var coordinateTransform = new CoordinateTransform(source, target);

            var image = InputImage.FromMediaImage(proxy.Image, proxy.ImageInfo.RotationDegrees);
            var results = await _barcodeScanner.Process(image);

            var _barcodeResults = Methods.ProcessBarcodeResult(results, coordinateTransform);

            if (_cameraView.ForceInverted)
            {
                Methods.InvertLuminance(proxy.Image);
                image = InputImage.FromMediaImage(proxy.Image, proxy.ImageInfo.RotationDegrees);
                results = await _barcodeScanner.Process(image);

                _barcodeResults.UnionWith(Methods.ProcessBarcodeResult(results, coordinateTransform));
            }

            if (_barcodeResults is not null && _cameraView is not null)
                _cameraView.DetectionFinished(_barcodeResults);
        }
        catch (Java.Lang.Exception)
        {

        }
        catch (Exception)
        {

        }
        finally
        {
            SafeCloseImageProxy(proxy);
        }
    } 

    private static void SafeCloseImageProxy(IImageProxy proxy)
    {
        try
        {
            proxy?.Close();
        }
        catch (Exception) 
        {

        }
    }

}
