using System;
namespace NativeScanLib.Models
{
    public class PassportModel
    {
        public Names Names { get; set; }
        public string DocumentType { get; set; }
        public string DocumentNumber { get; set; }
        public string NationalNumber { get; set; }
        public Region CountyOfIssue { get; set; }
        public Region Nationality { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime ExpirartionDate { get; set; }
        public string Sex { get; set; }
        public bool Valid { get; set; }
    }

    public class Names {
        public string LastName { get; set; }
        public string[] FirstNames { get; set; }
    }

    public class Region {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
