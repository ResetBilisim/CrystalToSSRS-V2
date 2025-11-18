using System;
using System.IO;
using System.Text;

namespace CrystalToSSRS.Testing
{
    /// <summary>
    /// Utility class for verifying UTF-8 BOM encoding in RDL files
    /// </summary>
    public class UTF8BOMVerifier
    {
        /// <summary>
        /// Verify if a file has UTF-8 BOM
        /// </summary>
        public static bool VerifyUTF8BOM(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"File not found: {filePath}");
                    return false;
                }

                byte[] buffer = new byte[3];
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    if (fs.Read(buffer, 0, 3) < 3)
                    {
                        Console.WriteLine("File too small to contain BOM");
                        return false;
                    }
                }

                // UTF-8 BOM: EF BB BF
                bool hasBom = buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF;

                if (hasBom)
                {
                    Console.WriteLine($"✓ UTF-8 BOM found in: {Path.GetFileName(filePath)}");
                }
                else
                {
                    Console.WriteLine($"✗ UTF-8 BOM NOT found in: {Path.GetFileName(filePath)}");
                    Console.WriteLine($"  Actual bytes: {buffer[0]:X2} {buffer[1]:X2} {buffer[2]:X2}");
                }

                return hasBom;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying BOM: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Verify XML declaration in file
        /// </summary>
        public static bool VerifyXMLDeclaration(string filePath)
        {
            try
            {
                string content = File.ReadAllText(filePath, Encoding.UTF8);
                
                if (content.StartsWith("<?xml") && content.Contains("encoding=\"utf-8\""))
                {
                    Console.WriteLine($"✓ XML declaration found in: {Path.GetFileName(filePath)}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"✗ XML declaration not found or encoding mismatch");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying XML declaration: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test a saved RDL file
        /// </summary>
        public static void TestRDLFile(string filePath)
        {
            Console.WriteLine($"\n=== Testing: {filePath} ===");
            
            bool bomOk = VerifyUTF8BOM(filePath);
            bool xmlOk = VerifyXMLDeclaration(filePath);
            
            if (bomOk && xmlOk)
            {
                Console.WriteLine("✓ File is correctly encoded and ready for SSRS");
            }
            else
            {
                Console.WriteLine("✗ File has encoding issues");
            }
        }

        /// <summary>
        /// Test all RDL files in a directory
        /// </summary>
        public static void TestDirectory(string directoryPath)
        {
            Console.WriteLine($"\n=== Testing directory: {directoryPath} ===\n");
            
            string[] rdlFiles = Directory.GetFiles(directoryPath, "*.rdl", SearchOption.AllDirectories);
            
            if (rdlFiles.Length == 0)
            {
                Console.WriteLine("No RDL files found");
                return;
            }

            int passCount = 0;
            int failCount = 0;

            foreach (string file in rdlFiles)
            {
                if (VerifyUTF8BOM(file))
                {
                    passCount++;
                }
                else
                {
                    failCount++;
                }
            }

            Console.WriteLine($"\n=== Summary ===");
            Console.WriteLine($"Total RDL files: {rdlFiles.Length}");
            Console.WriteLine($"Correct encoding: {passCount}");
            Console.WriteLine($"Incorrect encoding: {failCount}");
        }
    }

    // Example usage (for testing)
    /*
    class TestProgram
    {
        static void Main(string[] args)
        {
            // Test single file
            UTF8BOMVerifier.TestRDLFile("C:\\Reports\\Report1.rdl");
            
            // Test directory
            UTF8BOMVerifier.TestDirectory("C:\\Reports");
        }
    }
    */
}
