using System;
using System.IO;

namespace WpfExtensions.Helpers
{
  public class Utilities
  {
    /// <summary>
    /// Creates a relative path from one file or folder to another.
    /// </summary>
    /// <param name="relativeTo">Directory that defines the start of the relative path.</param>
    /// <param name="path">Contains the path that defines the endpoint of the relative path.</param>
    /// <returns>The relative path from the start directory to the end path or <c>toPath</c> if the paths are not related.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="UriFormatException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static string GetRelativePath(string relativeTo, string path)
    {
      if (string.IsNullOrEmpty(relativeTo)) throw new ArgumentNullException(nameof(relativeTo));
      if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

      if (!relativeTo.EndsWith($"{Path.DirectorySeparatorChar}") &&
          !relativeTo.EndsWith($"{Path.AltDirectorySeparatorChar}"))
      {
        relativeTo += Path.DirectorySeparatorChar;
      }

      Uri baseUri = new Uri(relativeTo);
      Uri fullUri = new Uri(path);

      if (baseUri.Scheme != fullUri.Scheme)
      {
        throw new InvalidOperationException($"The path '{path}' cannot made relative to {relativeTo}.");
      }

      var relativeUri = baseUri.MakeRelativeUri(fullUri);
      var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

      if (fullUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase))
      {
        relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
      }

      return relativePath;
    }
  }
}
