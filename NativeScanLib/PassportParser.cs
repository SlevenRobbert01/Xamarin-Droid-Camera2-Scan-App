using System;
using System.Text.RegularExpressions;
using NativeScanLib.Helpers;
using NativeScanLib.Models;

namespace NativeScanLib
{
    public class PassportParser
    {
        public enum PassportType
        {
            Undefined = 0,
            Type1 = 1,
            Type2 = 2,
            Type3 = 3
        }

        public static PassportModel ParsePassport(string text)
        {
            Regex MrzType1Regex = new Regex("([P])([A-Z0-9<])([A-Z]{3})([A-Z<]{39})\n([A-Z0-9<]{9})([0-9])([A-Z]{3})([0-9]{6})([0-9])([A-Z<])([0-9]{6})([0-9])([A-Z0-9<]{14})([0-9])([0-9])");
            Regex MrzType2Regex = new Regex("([V])([A-Z0-9<])([A-Z]{3})([A-Z<]{31})\n([A-Z0-9<]{9})([0-9])([A-Z]{3})([0-9]{6})([0-9])([A-Z<])([0-9]{6})([0-9])([A-Z0-9<]{8})");
            Regex MrzType3Regex = new Regex("([A-Z]{5})([0-9<]{25})\n([0-9]{6})([0-9]{1})([A-Z]{1})([0-9]{6})([0-9]{1})([A-Z<]{3})([0-9]{12})\n([A-Z<]{30})");

            Regex MrzType1Line1 = new Regex("([P])([A-Z0-9<])([A-Z]{3})([A-Z<]{39})\n");
            Regex MrzType1Line2 = new Regex("([A-Z0-9<]{9})([0-9])([A-Z]{3})([0-9]{6})([0-9])([A-Z<])([0-9]{6})([0-9])([A-Z0-9<]{14})([0-9])([0-9])");

            Regex MrzType2Line1 = new Regex("([A-Z])([A-Z0-9<])([A-Z]{3})([A-Z<]{31})");
            Regex MrzType2Line2 = new Regex("([A-Z0-9<]{9})([0-9])([A-Z]{3})([0-9]{6})([0-9])([A-Z<])([0-9]{6})([0-9])([A-Z0-9<]{8})");

            Regex MrzType3Line1 = new Regex("([A-Z]{5})([0-9<]{25})");
            Regex MrzType3Line2 = new Regex("([0-9]{6})([0-9]{1})([A-Z]{1})([0-9]{6})([0-9]{1})([A-Z<]{3})([0-9]{12})");
            Regex MrzType3Line3 = new Regex("([A-Z<]{30})");

            var matchType1 = MrzType1Regex.Match(text);
            var matchType2 = MrzType2Regex.Match(text);
            var matchType3 = MrzType3Regex.Match(text);

            var matchType1Line1 = MrzType1Line1.Match(text);
            var matchType1Line2 = MrzType1Line2.Match(text);

            var matchType2Line1 = MrzType2Line1.Match(text);
            var matchType2Line2 = MrzType2Line2.Match(text);

            var matchType3Line1 = MrzType3Line1.Match(text);
            var matchType3Line2 = MrzType3Line2.Match(text);
            var matchType3Line3 = MrzType3Line3.Match(text);

            if(matchType1.Success)
            {
                return ParsePassportType1(matchType1.Value.Split('\n'));
            }
            if(matchType2.Success){
                return ParsePassportType2(matchType2.Value.Split('\n'));
            }
            if(matchType3.Success)
            {
                return ParsePassportType3(matchType3.Value.Split('\n'));
            }
            if(matchType1Line1.Success && matchType1Line2.Success)
            {
                return ParsePassportType1(new string[]{matchType1Line1.Value, matchType1Line2.Value});
            }
            if (matchType2Line1.Success && matchType2Line2.Success)
            {
                return ParsePassportType2(new string[] { matchType2Line1.Value, matchType2Line2.Value });
            }
            if(matchType3Line1.Success && matchType3Line2.Success && matchType3Line3.Success)
            {
                return ParsePassportType3(new string[]{matchType3Line1.Value, matchType3Line2.Value,matchType3Line3.Value});
            }

            return null;
        }

        static PassportModel ParsePassportType1(string[] lines){
            var result = new PassportModel();
            var line0 = lines[0];
            var line1 = lines[1];

            try
            {
                /*DocumentType*/
                var documentType = PassportParserHelper.StripPadding(line0.Substring(0, 2));
                /*CountryOfIssue*/
                var countryOfIssue = PassportParserHelper.GetRegion(line0.Substring(2, 3));
                /*Names*/
                var names = PassportParserHelper.GetNames(line0.Substring(5));
                /*DocumentNr*/
                var documentNrRaw = line1.Substring(0, 9);
                var documentCheckDigit = line1.Substring(9, 1);
                var checkDigitVerifyDocumentNr = PassportParserHelper.CheckDigitVerify(documentNrRaw, int.Parse(documentCheckDigit));
                /*Nationality*/
                var nationality = PassportParserHelper.GetRegion(line1.Substring(10, 3));
                /*DateOfBirth*/
                var dobRaw = line1.Substring(13, 6);
                var dobCheckDigit = line1.Substring(19, 1);
                var checkDigitVerifyDob = PassportParserHelper.CheckDigitVerify(dobRaw, int.Parse(dobCheckDigit));
                var dateOfBirth = PassportParserHelper.GetFullDate(dobRaw);
                /*Sex*/
                var sex = line1.Substring(20, 1);
                /*ExpirationDate*/
                var expirationDateRaw = line1.Substring(21, 6);
                var expirationDateDigit = line1.Substring(27, 1);
                var checkDigitVerifyExpirationDate = PassportParserHelper.CheckDigitVerify(expirationDateRaw, int.Parse(expirationDateDigit));
                var expirationDate = PassportParserHelper.GetFullDate(expirationDateRaw);
                /*NationalNumber*/
                var nationalNumberRaw = line1.Substring(28, 14);
                var nationalNumber = PassportParserHelper.StripPadding(nationalNumberRaw);
                var nationalNumberCheckDigit = line1.Substring(42, 1);
                var checkDigitVerifyNationalNumber = PassportParserHelper.CheckDigitVerify(nationalNumberRaw, int.Parse(nationalNumberCheckDigit));
                /*Total*/
                var totalCheckDigit = line1.Substring(43, 1);
                var checkDigitVerifyTotal = PassportParserHelper.CheckDigitVerify(line0 + line1, int.Parse(totalCheckDigit));

                result = new PassportModel
                {
                    DocumentType = documentType,
                    CountyOfIssue = countryOfIssue,
                    DocumentNumber = PassportParserHelper.StripPadding(documentNrRaw),
                    DateOfBirth = dateOfBirth,
                    Sex = sex,
                    ExpirartionDate = expirationDate,
                    Nationality = nationality,
                    NationalNumber = nationalNumber,
                    Names = names,
                    Valid = (
                        checkDigitVerifyDocumentNr &&
                        checkDigitVerifyDob &&
                        checkDigitVerifyExpirationDate &&
                        checkDigitVerifyNationalNumber
                    )
                };

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("the scanned text is not a valid MRZ, " + ex.Message);
            }

            return result;
        }

        static PassportModel ParsePassportType2(string[] lines)
        {
            var result = new PassportModel();
            var line0 = lines[0];
            var line1 = lines[1];

            try
            {
                /*DocumentType*/
                var documentType = PassportParserHelper.StripPadding(line0.Substring(0, 2));
                /*CountryOfIssue*/
                var countryOfIssue = PassportParserHelper.GetRegion(line0.Substring(2, 3));
                /*Names*/
                var names = PassportParserHelper.GetNames(line0.Substring(5));
                /*DocumentNr*/
                var documentNrRaw = line1.Substring(0, 9);
                var documentCheckDigit = line1.Substring(9, 1);
                var checkDigitVerifyDocumentNr = PassportParserHelper.CheckDigitVerify(documentNrRaw, int.Parse(documentCheckDigit));
                /*Nationality*/
                var nationality = PassportParserHelper.GetRegion(line1.Substring(10, 3));
                /*DateOfBirth*/
                var dobRaw = line1.Substring(13, 6);
                var dobCheckDigit = line1.Substring(19, 1);
                var checkDigitVerifyDob = PassportParserHelper.CheckDigitVerify(dobRaw, int.Parse(dobCheckDigit));
                var dateOfBirth = PassportParserHelper.GetFullDate(dobRaw);
                /*Sex*/
                var sex = line1.Substring(20, 1);
                /*ExpirationDate*/
                var expirationDateRaw = line1.Substring(21, 6);
                var expirationDateDigit = line1.Substring(27, 1);
                var checkDigitVerifyExpirationDate = PassportParserHelper.CheckDigitVerify(expirationDateRaw, int.Parse(expirationDateDigit));
                var expirationDate = PassportParserHelper.GetFullDate(expirationDateRaw);

                result = new PassportModel
                {
                    DocumentType = documentType,
                    CountyOfIssue = countryOfIssue,
                    DocumentNumber = PassportParserHelper.StripPadding(documentNrRaw),
                    DateOfBirth = dateOfBirth,
                    Sex = sex,
                    ExpirartionDate = expirationDate,
                    Nationality = nationality,
                    Names = names,
                    Valid = (
                        checkDigitVerifyDocumentNr &&
                        checkDigitVerifyDob &&
                        checkDigitVerifyExpirationDate
                    )
                };

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("the scanned text is not a valid MRZ, " + ex.Message);
            }

            return result;
        }

        static PassportModel ParsePassportType3(string[] lines){
            var result = new PassportModel();

            var line0 = lines[0];
            var line1 = lines[1];
            var line2 = lines[2];

            try
            {
                /*DocumentType*/
                var documentType = PassportParserHelper.StripPadding(line0.Substring(0, 2));
                /*CountryOfIssue*/
                var countryOfIssue = PassportParserHelper.GetRegion(line0.Substring(2,3));
                /*DocumentNr*/
                var documentNrRaw = line0.Substring(5, 13);
                var documentCheckDigit = line0.Substring(18, 1);
                var checkDigitVerifyDocumentNr = PassportParserHelper.CheckDigitVerify(documentNrRaw, int.Parse(documentCheckDigit));
                /*DateOfBirth*/
                var dobRaw = line1.Substring(0, 6);
                var dobCheckDigit = line1.Substring(6, 1);
                var checkDigitVerifyDob = PassportParserHelper.CheckDigitVerify(dobRaw, int.Parse(dobCheckDigit));
                var dateOfBirth = PassportParserHelper.GetFullDate(dobRaw);
                /*Sex*/
                var sex = line1.Substring(7, 1);
                /*ExpirationDate*/
                var expirationDateRaw = line1.Substring(8, 6);
                var expirationDateDigit = line1.Substring(14, 1);
                var checkDigitVerifyExpirationDate = PassportParserHelper.CheckDigitVerify(expirationDateRaw, int.Parse(expirationDateDigit));
                var expirationDate = PassportParserHelper.GetFullDate(expirationDateRaw);
                /*Nationality*/
                var nationality = PassportParserHelper.GetRegion(line1.Substring(15, 3));
                /*NationalNumber*/
                var nationalNumber = line1.Substring(18, 11);
                var nationalNumberCheckDigit = line1.Substring(29, 1);
                var checkDigitVerifyNationalNumber = PassportParserHelper.CheckDigitVerify(nationalNumber, int.Parse(nationalNumberCheckDigit));
                /*Names*/
                var names = PassportParserHelper.GetNames(line2);

                result = new PassportModel
                {
                    DocumentType = documentType,
                    CountyOfIssue = countryOfIssue,
                    DocumentNumber = PassportParserHelper.StripPadding(documentNrRaw),
                    DateOfBirth = dateOfBirth,
                    Sex = sex,
                    ExpirartionDate = expirationDate,
                    Nationality = nationality,
                    NationalNumber = nationalNumber,
                    Names = names,
                    Valid = (
                        checkDigitVerifyDocumentNr && 
                        checkDigitVerifyDob && 
                        checkDigitVerifyExpirationDate && 
                        checkDigitVerifyNationalNumber
                    )
                };

            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("the scanned text is not a valid MRZ, " + ex.Message);
            }

            return result;
        }

        static bool CheckLenghtLines(string[] lines, int length)
        {
            bool result = true;
            foreach(var line in lines){
                result = result && line.Length >= length;
            }
            return result;
        }


    }
}
