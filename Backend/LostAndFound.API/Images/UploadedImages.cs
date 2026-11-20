using LostAndFound.Api.ErrorHandling;

namespace LostAndFound.Api.Images;

/// <summary>Runs uploaded files through the image processor and reports problems as field errors.</summary>
internal static class UploadedImages
{
    /// <summary>
    /// Processes every file before anything is stored, so one bad file rejects the whole request.
    /// Errors are keyed by position, for example "Images[1]".
    /// </summary>
    public static async Task<IReadOnlyList<ProcessedImage>> ProcessAllAsync(
        this IImageProcessor processor, IReadOnlyList<IFormFile> files, string field, CancellationToken cancellationToken)
    {
        var processed = new List<ProcessedImage>(files.Count);
        var errors = new Dictionary<string, string[]>();

        for (int index = 0; index < files.Count; index++)
        {
            try
            {
                processed.Add(await processor.ProcessOneAsync(files[index], cancellationToken));
            }
            catch (InvalidImageException exception)
            {
                errors[$"{field}[{index}]"] = [exception.Message];
            }
        }

        return errors.Count == 0 ? processed : throw new RequestValidationException(errors);
    }

    public static async Task<ProcessedImage> ProcessAsync(
        this IImageProcessor processor, IFormFile file, string field, CancellationToken cancellationToken)
    {
        try
        {
            return await processor.ProcessOneAsync(file, cancellationToken);
        }
        catch (InvalidImageException exception)
        {
            throw new RequestValidationException(field, exception.Message);
        }
    }

    private static async Task<ProcessedImage> ProcessOneAsync(this IImageProcessor processor, IFormFile file, CancellationToken cancellationToken)
    {
        // The declared length is checked first so an oversized upload is not read at all.
        if (file.Length > ImageLimits.MaxFileBytes)
        {
            throw new InvalidImageException($"Each image must be {ImageLimits.MaxFileBytes / (1024 * 1024)} MB or smaller.");
        }

        await using Stream stream = file.OpenReadStream();
        return await processor.ProcessAsync(stream, cancellationToken);
    }
}
