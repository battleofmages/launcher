public static class StringExtensions {
	// UnixPath
	public static string UnixPath(this string input) {
		return input;
	}

	// WindowsPath
	public static string WindowsPath(this string input) {
		return input.Replace("/", "\\");
	}
}