using System;
using System.Collections.Generic;
using System.Text;

namespace ROMHub.Models
{
    public class Rom
    {
        public int Id { get; set; }

        public string Title { get; set; } //Title of ROM

        public string Platform { get; set; } //Emulator for ROM

        public string FilePath { get; set; } //File Location of ROM

        public string CoverImagePath { get; set; } //Path to the cover image

        public bool IsFavourite { get; set; } //If the ROM is set as favourite
    }
}