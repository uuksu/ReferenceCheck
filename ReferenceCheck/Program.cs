using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.IO;

namespace ReferenceCheck
{
    class Program
    {
        public static bool printReferences = false;
        public static bool PrintAsOneliners = false;
        public static bool checkDirectory = false;
        public static string targetPath;
        public static int totalInvalids = 0;
        public static List<string> invalidProjects = new List<string>();

        /// <summary>
        /// Flags:
        ///     -l      List all references
        ///     -lo     List all references as one liners
        ///     -d      Use directory as input
        /// </summary>
        /// <param name="args">The args.</param>
        static void Main(string[] args)
        {
            // If something goes wrong with arguments
            if (ProcessArguments(args) == false)
            {
                Console.WriteLine("\nReference checker");
                Console.WriteLine("\nUsage: ");
                Console.WriteLine("\tReferenceCheck [file/directory] flags");
                Console.WriteLine("\nFlags:\n\t-l\tList all references\n\t-lo\tList all references as one liners\n\t-d\tUse in directory mode");
                
                System.Environment.Exit(-1);
            }

            // Checking if we should process directory or file
            if (checkDirectory)
            {
                ProcessDirectory(targetPath);
            }
            else
            {
                ProcessFile(targetPath);
            }
        }

        /// <summary>
        /// Processes the command line arguments.
        /// </summary>
        /// <param name="args">The args passed to main</param>
        public static bool ProcessArguments(string[] args)
        {
            // Failing if none arguments is given
            if (args.Count() == 0) { return false;  }

            if (args.Contains("-l")) { printReferences = true; }
            if (args.Contains("-lo")) { PrintAsOneliners = true; printReferences = true; }
            if (args.Contains("-d")) { checkDirectory = true; }

            // Failing if file or directory does not exists
            if (File.Exists(args[0]) || (Directory.Exists(args[0]) && checkDirectory ))
            {
                targetPath = args[0];
            }
            else
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Processes the whole directory.
        /// </summary>
        /// <param name="path">The path to the directory.</param>
        public static void ProcessDirectory(string path)
        {
            foreach (string d in Directory.GetDirectories(path))
            {
                foreach (string file in Directory.EnumerateFiles(d, "*.csproj", SearchOption.AllDirectories))
                {
                    ProcessFile(file);
                }
            }

            Console.WriteLine("\n== Directory check results ==");

            if (totalInvalids == 0)
            {
                Console.WriteLine("\nFound no reference errors.");
            }
            else
            {
                Console.WriteLine("\nFound {0} reference error(s):", totalInvalids);
                foreach (string invalidProject in invalidProjects)
                {
                    Console.WriteLine("\t{0}", invalidProject);
                }
            }
            
        }

        /// <summary>
        /// Processes single file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        public static void ProcessFile(string path)
        {
            var references = GetReferences(path);

            if (printReferences)
            {
                ListReferences(references);
            }

            CheckIfExists(references, Path.GetDirectoryName(path));
        }

        /// <summary>
        /// Lists the references.
        /// </summary>
        /// <param name="references">The references.</param>
        public static void ListReferences(IEnumerable<Reference> references)
        {
            foreach (var reference in references)
            {
                if (PrintAsOneliners)
                {
                    Console.WriteLine("{0} | {1}", reference.Include, reference.Path);
                    continue;
                }
                
                Console.WriteLine("\nName: " + reference.Include);
                Console.WriteLine("Path: " + reference.Path);
            }
        }

        /// <summary>
        /// Checks if referenced files exists.
        /// </summary>
        /// <param name="references">The references.</param>
        /// <param name="path">The root path of project</param>
        public static void CheckIfExists(IEnumerable<Reference> references, string path)
        {
            int invalidCounter = 0;

            string projectName = path.Split('\\').Last();

            Console.WriteLine("\nResults for project {0}: ", projectName);

            foreach (var reference in references)
            {
                if (reference.Path != null)
                {
                    // Checking if file exists
                    if (!File.Exists(Path.Combine(path, reference.Path)))
                    {
                        string invalidProject = reference.Include.Split(',')[0];

                        Console.WriteLine("\t* Missing reference for {0}.", invalidProject);
                        invalidProjects.Add(String.Format("{0}, {1}", projectName, invalidProject));
                        invalidCounter++;
                    }
                }
            }

            totalInvalids += invalidCounter;

            Console.WriteLine("\n\tProcessed {0} references where {1} was invalid.", references.Count(), invalidCounter);
        }

        /// <summary>
        /// Gets the references from specified project xml file.
        /// </summary>
        /// <param name="path">The path to the project xml file.</param>
        /// <returns></returns>
        public static IEnumerable<Reference> GetReferences(string path)
        {
            XDocument doc = XDocument.Load(path);

            // Searching for <Reference>
            var references = doc.Descendants().Where(i => i.NodeType == XmlNodeType.Element && i.Name.LocalName == "Reference")
                        .Select(n => new Reference
                        {
                            Include = n.Attribute("Include").Value,
                            Path = (n.Elements().Where(i => i.Name.LocalName == "HintPath").Count() != 0) ?
                                        n.Elements().Where(i => i.Name.LocalName == "HintPath").First().Value :
                                        null
                        });

            return references;
        }
    }
}
