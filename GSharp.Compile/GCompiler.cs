﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.CSharp;
using GSharp.Base.Utilities;

namespace GSharp.Compile
{
    public class GCompiler
    {
        #region 속성
        public string XAML
        {
            get
            {
                return Base64Decode(_XAML);
            }
            set
            {
                _XAML = Base64Encode(value);
            }
        }
        private string _XAML;

        public string Source { get; set; }

        public StringCollection References
        {
            get
            {
                return parameters.ReferencedAssemblies;
            }
        }

        public string Commons
        {
            get
            {
                return _Commons;
            }
            set
            {
                _Commons = value;
                foreach (string reference in GetDefaultReference())
                {
                    if (!IsNameContains(References, reference))
                    {
                        References.Add(reference);
                    }
                }
            }
        }
        private string _Commons;

        public List<string> Dependencies { get; set; } = new List<string>();
        #endregion

        #region 객체
        private CSharpCodeProvider provider = new CSharpCodeProvider();
        private CompilerParameters parameters = new CompilerParameters();
        #endregion

        #region 생성자
        public GCompiler()
        {
            foreach (string reference in GetDefaultReference())
            {
                References.Add(reference);
            }
        }

        public GCompiler(string value) : this()
        {
            Source = value;
        }
        #endregion

        #region 내부 함수
        private string Base64Encode(string plainText)
        {
            if (plainText?.Length > 0)
            {
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
            }
            else
            {
                return string.Empty;
            }
        }

        private string Base64Decode(string base64EncodedData)
        {
            if (base64EncodedData?.Length > 0)
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(base64EncodedData));
            }
            else
            {
                return string.Empty;
            }
        }

        private bool IsNameContains(StringCollection references, string name)
        {
            return (from string dll
                    in references
                    where Path.GetFileName(dll) == Path.GetFileName(name)
                    select dll).ToArray().Count() > 0;
        }

        private string GetPublicKeyToken(AssemblyName assembly)
        {
            StringBuilder builder = new StringBuilder();
            byte[] token = assembly.GetPublicKeyToken();

            for (int i = 0; i < token.GetLength(0); i++)
            {
                builder.AppendFormat("{0:x2}", token[i]);
            }

            return builder.ToString();
        }

        private List<string> GetDefaultReference()
        {
            List<string> result = new List<string>();

            result.Add("System.dll");
            result.Add("System.Linq.dll");
            if (Commons?.Length > 0)
            {
                result.Add(Path.Combine(Commons, "GSharp.Extension.dll"));
            }
            result.Add($@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Xaml.dll");
            result.Add($@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\WindowsBase.dll");
            result.Add($@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\PresentationCore.dll");
            result.Add($@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\PresentationFramework.dll");

            return result;
        }

        private string ConvertToFullSource(string source)
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("using System;");
            result.AppendLine("using System.Collections.Generic;");
            result.AppendLine("using System.Linq;");
            result.AppendLine("using System.Text;");
            result.AppendLine("using System.Threading.Tasks;");
            result.AppendLine("using System.Reflection;");
            result.AppendLine("using System.Windows;");
            result.AppendLine("using System.Windows.Markup;");
            result.AppendLine("using GSharp.Extension.Abstracts;");
            result.AppendLine();
            result.AppendLine("[assembly: AssemblyTitle(\"Title\")]");
            result.AppendLine("[assembly: AssemblyProduct(\"Product\")]");
            result.AppendLine("[assembly: AssemblyCompany(\"Company\")]");
            result.AppendLine("[assembly: AssemblyCopyright(\"Copyright\")]");
            result.AppendLine("[assembly: AssemblyTrademark(\"Trademark\")]");
            result.AppendLine("[assembly: AssemblyVersion(\"1.0.0.0\")]");
            result.AppendLine("[assembly: AssemblyFileVersion(\"1.0.0.0\")]");
            result.AppendLine();
            result.AppendLine("namespace GSharp.Default");
            result.AppendLine("{");
            result.AppendLine("    public partial class App : Application");
            result.AppendLine("    {");
            result.AppendLine("        [STAThread]");
            result.AppendLine("        public static void Main()");
            result.AppendLine("        {");
            result.AppendLine("            App app = new App();");
            result.AppendLine("            app.InitializeComponent();");
            result.AppendLine("            app.Run();");
            result.AppendLine("        }");
            result.AppendLine();
            if (XAML.Length > 0)
            {
                result.AppendLine("        public string Decode(string value)");
                result.AppendLine("        {");
                result.AppendLine("            if (value != null && value.Length > 0)");
                result.AppendLine("            {");
                result.AppendLine("                return Encoding.UTF8.GetString(Convert.FromBase64String(value));");
                result.AppendLine("            }");
                result.AppendLine("            else");
                result.AppendLine("            {");
                result.AppendLine("                return string.Empty;");
                result.AppendLine("            }");
                result.AppendLine("        }");
                result.AppendLine();
                result.AppendLine("        public GView FindControl(DependencyObject parent, string value)");
                result.AppendLine("        {");
                result.AppendLine("            return LogicalTreeHelper.FindLogicalNode(parent, value) as GView;");
                result.AppendLine("        }");
            }
            result.AppendLine();
            result.AppendLine("        public void InitializeComponent()");
            result.AppendLine("        {");
            if (XAML.Length > 0)
            {
                result.AppendLine($@"            Window window = (XamlReader.Parse(Decode(""{_XAML}"")) as Window);");
            }
            else
            {
                result.AppendLine("            Window window = new Window();");
                result.AppendLine("            window.Opacity = 0;");
                result.AppendLine("            window.WindowStyle = WindowStyle.None;");
                result.AppendLine("            window.AllowsTransparency = true;");
                result.AppendLine("            window.ShowInTaskbar = false;");
            }
            result.AppendLine("            window.Loaded += (s, e) => Initialize();");
            result.AppendLine("            window.Closing += (s, e) =>");
            result.AppendLine("            {");
            result.AppendLine("                if (Closing != null) Closing();");
            result.AppendLine("            };");
            result.AppendLine("            window.Show();");
            // 테스트
            result.AppendLine(@"            FindControl(window, ""MyTestName"").Click += () =>");
            result.AppendLine("            {");
            result.AppendLine(@"                MessageBox.Show(""클릭 이벤트 발동"");");
            result.AppendLine("            };");
            // 테스트
            result.AppendLine("        }");
            result.AppendLine();
            result.Append(ConvertAssistant.Indentation(source, 2));
            result.AppendLine("    }");
            result.AppendLine("}");

            return result.ToString();
        }

        private void CopyDirectory(string source, string destination, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(source);

            DirectoryInfo[] dirs = dir.GetDirectories();
            if (!Directory.Exists(destination))
            {
                Directory.CreateDirectory(destination);
            }

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destination, file.Name);
                file.CopyTo(temppath, true);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    CopyDirectory(subdir.FullName, Path.Combine(destination, subdir.Name), copySubDirs);
                }
            }
        }
        #endregion

        #region 사용자 함수
        /// <summary>
        /// 외부 참조를 추가합니다.
        /// </summary>
        /// <param name="path">외부 참조 파일의 경로입니다.</param>
        public void LoadReference(string path)
        {
            if (!IsNameContains(References, path))
            {
                // 참조 추가
                References.Add(path);

                // 참조의 종속성 검사
                foreach (AssemblyName assembly in Assembly.LoadFrom(path).GetReferencedAssemblies())
                {
                    // 참조 종속성 검사
                    string referencesName = null;
                    string dllPath = string.Format(@"{0}\{1}.dll", Path.GetDirectoryName(path), assembly.Name);
                    if (File.Exists(dllPath))
                    {
                        // 동일 경로에 존재
                        referencesName = dllPath;
                    }
                    else
                    {
                        // 동일 경로에 없음
                        // 외부 종속성 중복 검사
                        if ((from callingAssembly
                             in Assembly.GetCallingAssembly().GetReferencedAssemblies()
                             select callingAssembly.Name).Contains(assembly.Name))
                        {
                            continue;
                        }

                        // 글로벌 캐시에 존재 여부 검사
                        string[] dllGAC =
                            Directory.GetFiles
                            (
                                Environment.GetFolderPath(Environment.SpecialFolder.Windows) + @"\assembly", assembly.Name + ".dll",
                                SearchOption.AllDirectories
                            );
                        dllGAC = dllGAC.Where(dll => dll.IndexOf(GetPublicKeyToken(assembly)) != -1).ToArray();

                        if (dllGAC.Length > 0)
                        {
                            // 글로벌 캐시에 존재
                            // 시스템에 맞는 파일 검색
                            if (dllGAC.Length == 1)
                            {
                                referencesName = dllGAC.First();
                            }
                            else
                            {
                                referencesName = dllGAC.Where(dll => dll.IndexOf(Environment.Is64BitOperatingSystem ? "GAC_64" : "GAC_32") != -1).First();
                            }
                        }
                        else
                        {
                            referencesName = assembly.Name + ".dll";
                        }
                    }

                    // 참조의 종속성을 추가
                    if (!IsNameContains(References, referencesName))
                    {
                        References.Add(referencesName);
                    }
                }
            }
        }

        /// <summary>
        /// 외부 참조를 비동기로 추가합니다.
        /// </summary>
        /// <param name="path">외부 참조 파일의 경로입니다.</param>
        public async void LoadReferenceAsync(string path)
        {
            await Task.Run(() => LoadReference(path));
        }

        /// <summary>
        /// 실행시 필요한 추가 종속성을 추가합니다.
        /// </summary>
        /// <param name="path">추가 종속성 파일의 경로입니다.</param>
        public void LoadDependencies(string path)
        {
            if (!Dependencies.Contains(path))
            {
                Dependencies.Add(path);
            }
        }

        /// <summary>
        /// 실행시 필요한 추가 종속성을 비동기로 추가합니다.
        /// </summary>
        /// <param name="path">추가 종속성 파일의 경로입니다.</param>
        public async void LoadDependenciesAsync(string path)
        {
            await Task.Run(() => LoadDependencies(path));
        }

        /// <summary>
        /// 소스를 빌드하여 컴파일된 파일을 생성합니다.
        /// </summary>
        /// <param name="path">컴파일된 파일을 생성할 경로입니다.</param>
        /// <param name="isExecutable">실행 파일 형태로 컴파일 할지 여부를 설정합니다.</param>
        public GCompilerResults Build(string path, bool isExecutable = false)
        {
            parameters.OutputAssembly = path;
            parameters.GenerateExecutable = isExecutable;
            parameters.CompilerOptions = "/platform:x86 /target:winexe";
            string fullSource = ConvertToFullSource(Source);

            GCompilerResults results = new GCompilerResults
            {
                Source = fullSource,
                Results = provider.CompileAssemblyFromSource(parameters, fullSource)
            };

            foreach (string dll in References)
            {
                if (File.Exists(dll))
                {
                    File.Copy(dll, string.Format(@"{0}\{1}", Path.GetDirectoryName(path), Path.GetFileName(dll)), true);
                }
            }

            foreach (string directory in Dependencies)
            {
                if (Directory.Exists(directory))
                {
                    CopyDirectory(directory, Path.GetDirectoryName(path), true);
                }
            }

            return results;
        }

        /// <summary>
        /// 소스를 빌드하여 컴파일된 파일을 비동기로 생성합니다.
        /// </summary>
        /// <param name="path">컴파일된 파일을 생성할 경로입니다.</param>
        /// <param name="isExecutable">실행 파일 형태로 컴파일 할지 여부를 설정합니다.</param>
        public async Task<GCompilerResults> BuildAsync(string path, bool isExecutable = false)
        {
            return await Task.Run(() => Build(path, isExecutable));
        }
        #endregion
    }
}
