using System;
using System.Collections.Generic;
using System.Text;

namespace ROMHUB_2026.Models
{
    public class Rom
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Platform { get; set; }

        public string FilePath { get; set; }

        public string CoverImagePath { get; set; }

        public bool IsFavourite { get; set; }
    }
}