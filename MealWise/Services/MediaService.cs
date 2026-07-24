namespace MealWise.Services;

public class MediaService
{

    public async Task<byte[]?> TakePhotoAsync()
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported) return null;

            FileResult photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo == null) return null;

            using Stream stream = await photo.OpenReadAsync();

#if ANDROID

            return await ImageOptimizer.OptimizeImageAsync(stream);
#else
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return ms.ToArray();
#endif
        }
        catch (Exception ex)
        {

            System.Diagnostics.Debug.WriteLine($"Error taking photo: {ex.Message}");
            return null;
        }
    }

    public async Task<byte[]?> PickPhotoAsync()
    {
        try
        {
            FileResult photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo == null) return null;

            using Stream stream = await photo.OpenReadAsync();

#if ANDROID
            return await ImageOptimizer.OptimizeImageAsync(stream);
#else
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return ms.ToArray();
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error picking photo: {ex.Message}");
            return null;
        }
    }
}