namespace ROMHub.Models
{
    public class Emulator
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Platform { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public string ImagePath { get; set; } = string.Empty;

        // Optional argument template. Use "{rom}" where the selected ROM path should be inserted.
        public string Arguments { get; set; } = string.Empty;
    }
}
