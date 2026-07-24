#if ANDROID
using Android.Graphics;
using System.IO;

namespace MealWise.Services;

public static class ImageOptimizer
{
    /// <summary>
    /// Зменшує розміри зображення та стискає якість в JPEG.
    /// Перетворює фото 10 MB на якісний файл ~150-300 KB.
    /// </summary>
    public static async Task<byte[]> OptimizeImageAsync(Stream originalStream, int maxWidth = 1080, int maxHeight = 1080, int quality = 75)
    {
        using var memoryStream = new MemoryStream();
        await originalStream.CopyToAsync(memoryStream);
        byte[] imageBytes = memoryStream.ToArray();

        // 1. Декодуємо лише розміри, щоб розрахувати коефіцієнт зменшення (захист від OOM)
        var options = new BitmapFactory.Options { InJustDecodeBounds = true };
        BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length, options);

        options.InSampleSize = CalculateInSampleSize(options, maxWidth, maxHeight);
        options.InJustDecodeBounds = false;

        // 2. Завантажуємо зменшену копію в пам'ять
        Bitmap? originalBitmap = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length, options);
        if (originalBitmap == null) return imageBytes;

        // 3. Точний пропорційний ресайз
        Bitmap resizedBitmap = ScaleBitmap(originalBitmap, maxWidth, maxHeight);

        // 4. Компресія у JPEG з якістю 75%
        using var outputStream = new MemoryStream();
        resizedBitmap.Compress(Bitmap.CompressFormat.Jpeg!, quality, outputStream);

        // Звільняємо пам'ять Android
        originalBitmap.Recycle();
        if (resizedBitmap != originalBitmap) resizedBitmap.Recycle();

        return outputStream.ToArray();
    }

    private static int CalculateInSampleSize(BitmapFactory.Options options, int reqWidth, int reqHeight)
    {
        int height = options.OutHeight;
        int width = options.OutWidth;
        int inSampleSize = 1;

        if (height > reqHeight || width > reqWidth)
        {
            int halfHeight = height / 2;
            int halfWidth = width / 2;

            while ((halfHeight / inSampleSize) >= reqHeight && (halfWidth / inSampleSize) >= reqWidth)
            {
                inSampleSize *= 2;
            }
        }
        return inSampleSize;
    }

    private static Bitmap ScaleBitmap(Bitmap bitmap, int maxWidth, int maxHeight)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;

        if (width <= maxWidth && height <= maxHeight) return bitmap;

        float ratio = Math.Min((float)maxWidth / width, (float)maxHeight / height);
        int newWidth = (int)(width * ratio);
        int newHeight = (int)(height * ratio);

        return Bitmap.CreateScaledBitmap(bitmap, newWidth, newHeight, true)!;
    }
}
#endif