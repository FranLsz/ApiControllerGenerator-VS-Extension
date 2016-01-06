using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using ApiControllerGenerator.CodeSnippet;
using ApiControllerGenerator.Utils;
using EnvDTE;
using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet;
using NuGet.VisualStudio;
using Project = Microsoft.CodeAnalysis.Project;
using Solution = Microsoft.CodeAnalysis.Solution;

namespace ApiControllerGenerator.MainDialog
{
    public partial class MainDialog : Form
    {
        private IServiceProvider ServiceProvider { get; }

        public MainDialog(IServiceProvider svc)
        {
            ServiceProvider = svc;
            InitializeComponent();
        }

        private VisualStudioWorkspace GetWorkspace()
        {
            IComponentModel componentModel = this.ServiceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            return componentModel.GetService<Microsoft.VisualStudio.LanguageServices.VisualStudioWorkspace>();
        }

        private string GetHour()
        {
            return DateTime.Now.ToString("HH:mm:ss");
        }

        private void GetCurrentSolution(out Solution solution)
        {
            solution = GetWorkspace().CurrentSolution;
        }

        private void MainDialog_Load(object sender, EventArgs e)
        {
            //GenerateBtn.Visible = false;
            MinimizeBox = false;
            MaximizeBox = false;
        }
        // Send progress
        private void Send(BackgroundWorker worker, int progress)
        {
            worker.ReportProgress(progress);
        }
        // Send with text
        private void Send(BackgroundWorker worker, int progress, string text)
        {
            worker.ReportProgress(progress, new Tuple<string>(text));
        }
        // Send with text color
        private void Send(BackgroundWorker worker, int progress, string text, Color color)
        {
            worker.ReportProgress(progress, new Tuple<string, Color>(text, color));
        }

        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBar.Value = e.ProgressPercentage;

            if (e.UserState == null) return;

            if (e.UserState.GetType() == typeof(Tuple<string, Color>))
            {
                var tuple = (Tuple<string, Color>)e.UserState;
                LogBox.AppendLine(GetHour() + tuple.Item1, tuple.Item2);
            }
            else if (e.UserState.GetType() == typeof(Tuple<string>))
            {
                var tuple = (Tuple<string>)e.UserState;
                LogBox.AppendLine(GetHour() + tuple.Item1);
            }
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
                LogBox.AppendLine("Canceled!");

            else if (e.Error != null)
                LogBox.AppendLine(("Error: " + e.Error.Message), Color.Red);

            else
                LogBox.AppendLine("Done!");

            ExitBtn.Visible = true;
        }




        // GENERATE BUTTON
        private void GenerateBtn_Click(object sender, EventArgs e)
        {
            GeneratePanel.Visible = false;
            ProcessPanel.Visible = true;
            SettingsBtn.Visible = false;
            GoBackBtn.Visible = false;

            var apiProjectName = ApiProject.SelectedItem.ToString();
            var repositoryProjectName = RepositoryIncluded.Checked ? apiProjectName : RepositoryProject.SelectedItem.ToString();

            var options = new Dictionary<string, bool>
             {
                 {"BaseController", GenerateOptions_BaseController.Checked},
                 {"Unity", GenerateOptions_Unity.Checked},
                 {"CORS", GenerateOptions_CORS.Checked},
                 {"JSON", GenerateOptions_JSON.Checked}
             };

            BackgroundWorker bw = new BackgroundWorker
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = true
            };
            bw.DoWork += bw_DoWork;
            bw.ProgressChanged += bw_ProgressChanged;
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;

            var param = new Dictionary<string, object>
             {
                 {"ApiProjectName", apiProjectName},
                 {"RepositoryProjectName", repositoryProjectName},
                 {"Options", options}
             };

            if (bw.IsBusy != true)
            {
                bw.RunWorkerAsync(param);
            }
        }

        private async void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            if (worker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }

            // PARAMS
            var param = (Dictionary<string, object>)e.Argument;

            var apiProjectName = param["ApiProjectName"].ToString();
            var repositoryProjectName = param["RepositoryProjectName"].ToString();
            var options = (Dictionary<string, bool>)param["Options"];

            // VARIABLES 
            var bootstrapper = true;
            var sameProjects = repositoryProjectName == apiProjectName;
            var _onlineNugetPackageLocation = "https://packages.nuget.org/api/v2";
            //worker.ReportProgress(0, " ---ACG process start---");
            Send(worker, 0, " ---ACG process started---");
            //----------------------------------------------------------------------------------------------------------------
            // WORKSPACE GETTING
            Send(worker, 0, " - Trying to get workspace");
            var workspace = GetWorkspace();
            Send(worker, 5, " - Workspace loaded", Color.Green);

            //----------------------------------------------------------------------------------------------------------------
            // SOLUTION GETTING
            Send(worker, 5, " - Trying to get solution");
            Solution solution;
            GetCurrentSolution(out solution);
            Send(worker, 10, " - Current solution loaded", Color.Green);

            //----------------------------------------------------------------------------------------------------------------
            // API AND REPOSITORY PROJECT GETTING
            Send(worker, 10, " - Trying to get API project named '" + apiProjectName + "'");
            var apiProject = solution.Projects.First(o => o.Name == apiProjectName);
            if (apiProject != null)
                Send(worker, 15, " - '" + apiProjectName + "' project loaded", Color.Green);
            else
                Send(worker, 15, " - '" + apiProjectName + "' project not found", Color.Red);

            Project repositoryProject;
            // if the api and repository are different projects
            if (!sameProjects)
            {
                Send(worker, 15, " - Trying to get repository project named '" + repositoryProjectName + "'");
                repositoryProject = solution.Projects.First(o => o.Name == repositoryProjectName);
                if (apiProject != null)
                    Send(worker, 15, " - '" + repositoryProjectName + "' project loaded", Color.Green);
                else
                    Send(worker, 15, " - '" + repositoryProjectName + "' project not found", Color.Red);
            }
            // if are the same project
            else
            {
                repositoryProject = apiProject;
            }
            //----------------------------------------------------------------------------------------------------------------
            // AUTO INSTALL NUGET PACKAGES
            Send(worker, 25, " - Installing NuGet packages");
            // get project
            DTE dte = (DTE)this.ServiceProvider.GetService(typeof(DTE));
            Projects dteProjects = dte.Solution.Projects;
            EnvDTE.Project dteProject = null;

            for (int i = 1; i <= dteProjects.Count; i++)
            {
                if (dteProjects.Item(i).Name == apiProjectName)
                    dteProject = dteProjects.Item(i);
            }

            string packageID = "EntityFramework";

            //Connect to the official package repository
            IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository("https://packages.nuget.org/api/v2");

            //PackageManager packageManager = new PackageManager(repo, path);
            var componentModel = (IComponentModel)ServiceProvider.GetService(typeof(SComponentModel));
            IVsPackageInstaller pckInstaller = componentModel.GetService<IVsPackageInstaller>();

            var packagesToInstall = new Dictionary<string, SemanticVersion>();

            var packages = new Dictionary<string, string>();

            packages.Add("EntityFramework", "EntityFramework");

            if (options["Unity"])
                packages.Add("Unity", "Unity.WebApi");

            if (options["CORS"])
                packages.Add("CORS", "Microsoft.AspNet.WebApi.Cors");


            foreach (var pkg in packages)
            {
                List<IPackage> package = repo.FindPackagesById(pkg.Value).ToList();
                var lastVersion = package.Where(o => o.IsLatestVersion).Select(o => o.Version).FirstOrDefault();

                packagesToInstall.Add(pkg.Value, lastVersion);
            }


            foreach (var pkg in packagesToInstall)
            {
                Send(worker, 25, " - Installing " + pkg.Key + " " + pkg.Value.Version);
                try
                {
                    pckInstaller.InstallPackage(_onlineNugetPackageLocation, dteProject, pkg.Key, pkg.Value.Version, false);
                    Send(worker, 25, " - " + pkg.Key + " " + pkg.Value.Version + " installed", Color.Green);
                }
                catch (Exception)
                {
                    Send(worker, 25, " - Error on installing " + pkg.Key + " " + pkg.Value.Version, Color.Red);
                }
            }

            //----------------------------------------------------------------------------------------------------------------
            // CHECK REFERENCES INTEGRITY
            Send(worker, 35, " - Checking references integrity of '" + apiProjectName + "'");
            GetCurrentSolution(out solution);
            apiProject = solution.Projects.First(o => o.Name == apiProjectName);
            var allReferences = apiProject.MetadataReferences;

            var refStatus = new Dictionary<string, bool> { { "EntityFramework", false } };

            foreach (var op in options)
            {
                if (op.Key == "Unity" && op.Value)
                    refStatus.Add("Unity", false);
                else if (op.Key == "CORS" && op.Value)
                    refStatus.Add("CORS", false);
            }

            foreach (var reference in allReferences)
            {
                if (reference.Display.Contains("EntityFramework"))
                    refStatus["EntityFramework"] = true;

                if (refStatus.ContainsKey("Unity"))
                {
                    if (reference.Display.Contains("Unity.WebApi"))
                        refStatus["Unity"] = true;
                }

                if (refStatus.ContainsKey("CORS"))
                {
                    if (reference.Display.Contains("System.Web.Http.Cors"))
                        refStatus["CORS"] = true;
                }
            }
            var allRequiredRefsOk = true;
            foreach (var rs in refStatus)
            {
                if (rs.Key == "EntityFramework")
                {
                    if (rs.Value)
                        Send(worker, 40, " - EntityFramework reference checked", Color.Green);
                    else
                    {
                        allRequiredRefsOk = false;
                        Send(worker, 40, " - EntityFramework reference not found, please, download it in NuGet Package manager", Color.Red);
                    }
                }
                if (rs.Key == "Unity")
                {
                    if (rs.Value)
                        Send(worker, 40, " - Unity.WebApi reference checked", Color.Green);
                    else
                    {
                        allRequiredRefsOk = false;
                        Send(worker, 40, " - Unity.WebApi reference not found, please, download it in NuGet Package manager", Color.Red);
                    }
                }
                if (rs.Key == "CORS")
                {
                    if (rs.Value)
                        Send(worker, 40, " - CORS reference checked", Color.Green);
                    else
                    {
                        allRequiredRefsOk = false;
                        Send(worker, 40, " - CORS reference not found, please, download it in NuGet Package manager (Microsoft.AspNet.WebApi.Cors)", Color.Red);
                    }
                }
            }


            //----------------------------------------------------------------------------------------------------------------
            // START TO GENERATE
            if (repositoryProject != null && apiProject != null && allRequiredRefsOk)
            {
                string dbContext = "";

                //----------------------------------------------------------------------------------------------------------------
                //CONNECTION STRINGS IMPORT
                try
                {
                    // only import if they are different projects
                    if (!sameProjects)
                    {
                        Send(worker, 50, " - Trying to import connection strings from '" +
                                          repositoryProjectName + "'");

                        // App.config file
                        var xmlRepDoc = new XmlDocument();
                        var apiRepPath =
                            Path.Combine(
                                solution.Projects.First(o => o.Name == repositoryProjectName)
                                    .FilePath.Replace(repositoryProjectName + ".csproj", ""), "App.Config");
                        xmlRepDoc.Load(apiRepPath);
                        var repConnectionStrings =
                            xmlRepDoc.DocumentElement.ChildNodes.Cast<XmlElement>()
                                .First(x => x.Name == "connectionStrings");

                        // if App.config contains connectionStrings element
                        if (repConnectionStrings != null)
                        {
                            var csdata = repConnectionStrings.ChildNodes.Cast<XmlElement>().First(x => x.Name == "add");

                            // Web.config file
                            var xmlApiDoc = new XmlDocument();
                            var apiApiPath =
                                Path.Combine(
                                    solution.Projects.First(o => o.Name == apiProjectName)
                                        .FilePath.Replace(apiProjectName + ".csproj", ""), "Web.config");
                            xmlApiDoc.Load(apiApiPath);
                            var apiConnectionStrings =
                                xmlApiDoc.DocumentElement.ChildNodes.Cast<XmlElement>()
                                    .FirstOrDefault(x => x.Name == "connectionStrings");

                            // DbContext getting
                            dbContext = csdata.GetAttribute("name");
                            Send(worker, 50, " - Connection strings loaded with '" + dbContext + "' as DbContext ",
                                Color.Green);
                            Send(worker, 50, " - Trying to import connection strings  on '" +
                                              apiProjectName + "'");

                            var csnode = xmlApiDoc.ImportNode(repConnectionStrings, true);

                            // if API Web.config doesnt contain any connectionStrings element
                            if (apiConnectionStrings == null)
                            {
                                xmlApiDoc.DocumentElement.AppendChild(csnode);
                            }
                            // if Web.config alrealy contains that element
                            else
                            {
                                var addElement =
                                    apiConnectionStrings.ChildNodes.Cast<XmlElement>()
                                        .FirstOrDefault(x => x.Name == "add");
                                // if contains 'add' element
                                if (addElement != null)
                                {
                                    // if 'add' elements name  is the same as dbContext
                                    if (addElement.GetAttribute("name").Equals(dbContext))
                                        Send(worker, 55,
                                             " - API Web.Config file already contains a 'connectionStrings' element named '" +
                                            dbContext + "'", Color.Orange);
                                    else
                                    {
                                        apiConnectionStrings.AppendChild(xmlApiDoc.ImportNode(csdata, true));
                                    }
                                }
                                else
                                {
                                    apiConnectionStrings.AppendChild(xmlApiDoc.ImportNode(csdata, true));
                                }
                            }

                            xmlApiDoc.Save(apiApiPath);
                            Send(worker, 60, " - Connection strings successfully imported to '" + apiProjectName + "'",
                                Color.Green);
                        }
                        else
                        {
                            Send(worker, 60, " - Connection strings not found in App.config file of '" +
                                repositoryProjectName + "' project", Color.Red);
                        }
                    }
                    else
                    {
                        var xmlApiDoc = new XmlDocument();
                        var apiApiPath =
                            Path.Combine(
                                solution.Projects.First(o => o.Name == apiProjectName)
                                    .FilePath.Replace(apiProjectName + ".csproj", ""), "Web.config");
                        xmlApiDoc.Load(apiApiPath);
                        var apiConnectionStrings =
                            xmlApiDoc.DocumentElement.ChildNodes.Cast<XmlElement>()
                                .FirstOrDefault(x => x.Name == "connectionStrings");
                        var addElement = apiConnectionStrings.ChildNodes.Cast<XmlElement>().FirstOrDefault(x => x.Name == "add");
                        dbContext = addElement.GetAttribute("name");
                    }
                }
                catch (Exception)
                {
                    Send(worker, 60, " - Problems to import connection strings from '" + repositoryProjectName + "' to '" + apiProjectName + "' , make sure that Repository project contains App.Config file with 'connectionStrings' element", Color.Red);
                }


                CodeSnippets.ApiProjectName = apiProjectName;
                CodeSnippets.RepositoryProjectName = repositoryProjectName;

                //----------------------------------------------------------------------------------------------------------------
                // ADD REPOSITORY PROJECT REFERENCE
                if (!sameProjects)
                {
                    Send(worker, 65, " - Trying to reference '" + repositoryProjectName + "' on '" + apiProjectName + "'");

                    var alreadyReference = apiProject.ProjectReferences.Any(o => o.ProjectId == repositoryProject.Id);

                    if (!alreadyReference)
                    {
                        var projectWithReference = apiProject.AddProjectReference(new ProjectReference(repositoryProject.Id));
                        var res = workspace.TryApplyChanges(projectWithReference.Solution);
                        if (res)
                            Send(worker, 65, " - Reference added successfully", Color.Green);
                        else
                            Send(worker, 65, " - Can't add the reference, you must add it manually after process end",
                                Color.Red);
                    }
                    else
                    {
                        Send(worker, 65, " - Reference was already added before process start", Color.Orange);
                    }
                }

                //----------------------------------------------------------------------------------------------------------------
                // GET REPOSITORY VIEWMODELS
                var viewModels = repositoryProject.Documents.Where(o => o.Folders.Contains("ViewModels") && o.Name != "IViewModel.cs");
                Send(worker, 70, " - Trying to get all ViewModels");
                if (viewModels.Any())
                {
                    Send(worker, 75, " - ViewModels loaded", Color.Green);
                    // save here all classnames to create the bootstraper file with unity registerType
                    var classesNameList = new List<string>();

                    // max number of PK from one properties
                    var maxPkSize = 1;

                    //Get the PK (name - type) of the current ViewModel and creates his controller

                    //----------------------------------------------------------------------------------------------------------------
                    // CONTROLLERS GENERATE
                    Send(worker, 80, " - Trying to generate all controllers");

                    // if BaseController is enabled
                    if (options["BaseController"])
                    {
                        var controllerName = "BaseController";
                        CodeSnippets.ControllerInheritance = controllerName;
                        GetCurrentSolution(out solution);
                        apiProject = solution.Projects.First(o => o.Name == apiProjectName);
                        var res = apiProject.AddDocument(controllerName, CodeSnippets.GetBaseController(controllerName), new[] { apiProjectName, "Controllers" });
                        workspace.TryApplyChanges(res.Project.Solution);
                    }

                    foreach (var vm in viewModels)
                    {
                        GetCurrentSolution(out solution);
                        apiProject = solution.Projects.First(o => o.Name == apiProjectName);

                        var data = await vm.GetSemanticModelAsync();
                        var currentClass = data.SyntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
                        var className = currentClass.Identifier.Text.Replace("ViewModel", "");

                        var methods = data.SyntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>();
                        var getKeysMethod = methods.First(o => o.Identifier.Text.Equals("GetKeys"));
                        var methodBody = getKeysMethod.Body.GetText().ToString();
                        var i1 = methodBody.IndexOf("{", methodBody.IndexOf("{") + 1) + 1;
                        var i2 = methodBody.IndexOf("}", methodBody.IndexOf("}"));
                        var pks = methodBody.Substring(i1, i2 - i1).Replace(" ", "").Split(',');

                        if (pks.Count() > maxPkSize)
                            maxPkSize = pks.Count();

                        var props = data.SyntaxTree.GetRoot().DescendantNodes().OfType<PropertyDeclarationSyntax>();
                        var primaryKeysList = new List<Tuple<string, string>>();
                        foreach (var p in props)
                        {
                            var pname = p.Identifier.Text;
                            var pline = p.GetText().ToString();
                            var pk = pks.FirstOrDefault(o => o.Equals(pname));
                            if (pk != null)
                            {
                                var ptype = pline.Substring(pline.IndexOf("public ") + 7, pline.IndexOf(" " + pk) - pline.IndexOf("public ") - 7);
                                primaryKeysList.Add(new Tuple<string, string>(pname, ptype));
                            }
                        }

                        // add controller
                        var res = apiProject.AddDocument(className + "Controller", CodeSnippets.GetRepositoryController(className, primaryKeysList, options["CORS"], options["Unity"], dbContext), new[] { apiProjectName, "Controllers" });

                        workspace.TryApplyChanges(res.Project.Solution);

                        classesNameList.Add(className);
                        Send(worker, 80, " - " + className + "Controller generated", Color.Green);
                    }
                    Send(worker, 85, " - All controllers generated successfully", Color.Green);

                    //----------------------------------------------------------------------------------------------------------------
                    // CREATE BOOTSTRAPPER FILE AND REGYSTERTYPES
                    if (options["Unity"] && bootstrapper)
                    {
                        Send(worker, 90, " - Trying to create Bootstrapper file");

                        GetCurrentSolution(out solution);
                        apiProject = solution.Projects.First(o => o.Name == apiProjectName);

                        var newFile = apiProject.AddDocument("Bootstrapper",
                            CodeSnippets.GetBootstrapper(classesNameList, dbContext),
                            new[] { apiProjectName, "App_Start" });
                        workspace.TryApplyChanges(newFile.Project.Solution);

                        GetCurrentSolution(out solution);
                        apiProject = solution.Projects.First(o => o.Name == apiProjectName);
                        Send(worker, 90, " - Bootstraper file created", Color.Green);
                        Send(worker, 90, " - Added all registerType statements for each entity", Color.Green);

                        // adds "Bootstrapper.InitUnity(container);" line in unity config
                        foreach (ProjectItem pi in dteProject.ProjectItems)
                        {
                            if (pi.Name != "App_Start") continue;
                            foreach (ProjectItem subpi in pi.ProjectItems)
                            {
                                if (subpi.Name != "UnityConfig.cs") continue;
                                // DELETE FILE
                                var filename = subpi.FileNames[0];
                                subpi.Remove();
                                System.IO.File.Delete(filename);
                            }
                        }
                        GetCurrentSolution(out solution);
                        apiProject = solution.Projects.First(o => o.Name == apiProjectName);
                        var res = apiProject.AddDocument("UnityConfig", CodeSnippets.GetUnityConfig(true), new[] { apiProjectName, "App_Start" });
                        workspace.TryApplyChanges(res.Project.Solution);
                        /* var unityConfigDoc =
                             apiProject.Documents.First(
                                 o => o.Folders.Contains("App_Start") && o.Name == "UnityConfig.cs");
                         var tree = await unityConfigDoc.GetSyntaxTreeAsync();

                         var targetBlock =
                             tree.GetRoot()
                                 .DescendantNodes()
                                 .OfType<BlockSyntax>()
                                 .FirstOrDefault(
                                     x =>
                                         x.Statements.Any(
                                             y => y.ToString().Contains("var container = new UnityContainer();")));

                         StatementSyntax syn1 =
                             SyntaxFactory.ParseStatement(@"
             Bootstrapper.InitUnity(container);

 ");
                         List<StatementSyntax> newSynList = new List<StatementSyntax> { syn1 };

                         SyntaxList<StatementSyntax> blockWithNewStatements = targetBlock.Statements;

                         foreach (var syn in newSynList)
                         {
                             blockWithNewStatements = blockWithNewStatements.Insert(1, syn);
                         }

                         BlockSyntax newBlock = SyntaxFactory.Block(blockWithNewStatements);

                         var newRoot = tree.GetRoot().ReplaceNode(targetBlock, newBlock);

                         var doc = unityConfigDoc.WithSyntaxRoot(newRoot);
                         workspace.TryApplyChanges(doc.Project.Solution);
                         */
                        GetCurrentSolution(out solution);
                        apiProject = solution.Projects.First(o => o.Name == apiProjectName);
                        Send(worker, 90, " - Added call to Bootstrapper init in UnityConfig.cs file", Color.Green);
                    }

                    //WEBAPI.CONFIG FILE
                    // adds unity init, json formatter and url mapping line in web config
                    Send(worker, 95, " - Trying to add configuration statements on WebApiConfig.cs");

                    foreach (ProjectItem pi in dteProject.ProjectItems)
                    {
                        if (pi.Name != "App_Start") continue;
                        foreach (ProjectItem subpi in pi.ProjectItems)
                        {
                            if (subpi.Name != "WebApiConfig.cs") continue;
                            // DELETE FILE
                            var filename = subpi.FileNames[0];
                            subpi.Remove();
                            System.IO.File.Delete(filename);
                        }
                    }
                    GetCurrentSolution(out solution);
                    apiProject = solution.Projects.First(o => o.Name == apiProjectName);

                    var config = "";

                    if (options["Unity"])
                    {
                        config += @"
            UnityConfig.RegisterComponents();
 ";
                        Send(worker, 95, " - Added component register of Unity", Color.Green);
                    }
                    if (options["JSON"])
                    {
                        config += @"
            var json = config.Formatters.JsonFormatter;
            json.SerializerSettings.PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects;
            config.Formatters.Remove(config.Formatters.XmlFormatter);
 ";
                        Send(worker, 95, " - Added JSON formatter", Color.Green);
                    }
                    if (options["CORS"])
                    {
                        config += @"
            config.EnableCors();
 ";
                    }
                    Send(worker, 95, " - Enabled CORS header", Color.Green);
                    var routeTemplate = "";
                    var defaults = "";
                    for (var i = 1; i <= maxPkSize; i++)
                    {
                        if (i == 1)
                        {
                            routeTemplate += "{id}";
                            defaults += "id = RouteParameter.Optional";
                            continue;
                        }
                        routeTemplate += "/{id" + i + "}";
                        defaults += ", id" + i + " = RouteParameter.Optional";
                    }
                    var routing = @"
            config.Routes.MapHttpRoute(
                name: ""DefaultApi"",
                routeTemplate: ""api/{controller}/" + routeTemplate + @""",
                defaults: new { " + defaults + @" }
            );
 ";
                    var resWac = apiProject.AddDocument("WebApiConfig", CodeSnippets.GetWebApiConfig(config, routing), new[] { apiProjectName, "App_Start" });
                    workspace.TryApplyChanges(resWac.Project.Solution);

                    /*var webApiConfigDoc = apiProject.Documents.First(o => o.Folders.Contains("App_Start") && o.Name == "WebApiConfig.cs");
                    var webApitree = await webApiConfigDoc.GetSyntaxTreeAsync();

                    var targetBlock2 =
                        webApitree.GetRoot()
                            .DescendantNodes()
                            .OfType<BlockSyntax>()
                            .FirstOrDefault(x => x.Statements.Any(y => y.ToString().Contains("config.MapHttpAttributeRoutes();")));


                    Send(worker, 95, " - Enabled CORS", Color.Green);
                }

                StatementSyntax syn2 = SyntaxFactory.ParseStatement(config);

                StatementSyntax syn3 =
                    SyntaxFactory.ParseStatement(@"
         config.Routes.MapHttpRoute(
             name: ""DefaultApi"",
             routeTemplate: ""api/{controller}/" + routeTemplate + @""",
             defaults: new { " + defaults + @" }
         );
");
                List<StatementSyntax> newSynList2 = new List<StatementSyntax> { syn2 };

                var r = targetBlock2.Statements.RemoveAt(1);

                SyntaxList<StatementSyntax> blockWithNewStatements2 = r;

                foreach (var syn in newSynList2)
                {
                    blockWithNewStatements2 = blockWithNewStatements2.Insert(0, syn);
                }
                blockWithNewStatements2 = blockWithNewStatements2.Insert(blockWithNewStatements2.Count, syn3);

                BlockSyntax newBlock2 = SyntaxFactory.Block(blockWithNewStatements2);

                var newRoot2 = webApitree.GetRoot().ReplaceNode(targetBlock2, newBlock2);

                var doc2 = webApiConfigDoc.WithSyntaxRoot(newRoot2);
                // workspace.TryApplyChanges(doc2.Project.Solution);
                */
                }
                else
                {
                    Send(worker, 100, " - ViewModels folder not found",
                                Color.Red);
                }
            }

            Send(worker, 100, "---ACG process ended---");
        }

        // ACG GENERATE PROCESS
        /*private async Task<bool> AcgGenerateProcess(string apiProjectName, string repositoryProjectName, Dictionary<string, bool> options)
        {
            var bootstrapper = true;
            var sameProjects = repositoryProjectName == apiProjectName;
            string _onlineNugetPackageLocation = "https://packages.nuget.org/api/v2";

            LogBox.AppendLine("---ACG process start---");
            //----------------------------------------------------------------------------------------------------------------
            // WORKSPACE GETTING
            LogBox.AppendLine(GetHour() + " - Trying to get workspace");
            var workspace = GetWorkspace();
            LogBox.AppendLine(GetHour() + " - Workspace loaded", Color.Green);
            ProgressBar.Value = 5;

            //----------------------------------------------------------------------------------------------------------------
            // SOLUTION GETTING
            LogBox.AppendLine(GetHour() + " - Trying to get solution");
            Solution solution;
            GetCurrentSolution(out solution);
            LogBox.AppendLine(GetHour() + " - Current solution loaded", Color.Green);
            ProgressBar.Value = 10;

            //----------------------------------------------------------------------------------------------------------------
            // API AND REPOSITORY PROJECT GETTING
            LogBox.AppendLine(GetHour() + " - Trying to get API project named '" + apiProjectName + "'");
            var apiProject = solution.Projects.First(o => o.Name == apiProjectName);
            if (apiProject != null)
                LogBox.AppendLine(GetHour() + " - '" + apiProjectName + "' project loaded", Color.Green);
            else
                LogBox.AppendLine(GetHour() + " - '" + apiProjectName + "' project not found", Color.Red);

            Project repositoryProject;
            // if the api and repository are different projects
            if (!sameProjects)
            {
                LogBox.AppendLine(GetHour() + " - Trying to get repository project named '" + repositoryProjectName + "'");
                repositoryProject = solution.Projects.First(o => o.Name == repositoryProjectName);
                if (apiProject != null)
                    LogBox.AppendLine(GetHour() + " - '" + repositoryProjectName + "' project loaded", Color.Green);
                else
                    LogBox.AppendLine(GetHour() + " - '" + repositoryProjectName + "' project not found", Color.Red);
            }
            // if are the same project
            else
            {
                repositoryProject = apiProject;
            }
            ProgressBar.Value = 15;
            //----------------------------------------------------------------------------------------------------------------
            // AUTO INSTALL NUGET PACKAGES
            LogBox.AppendLine(GetHour() + " - Installing NuGet packages");
            // get project
            DTE dte = (DTE)this.ServiceProvider.GetService(typeof(DTE));
            Projects dteProjects = dte.Solution.Projects;
            EnvDTE.Project dteProject = null;

            for (int i = 1; i <= dteProjects.Count; i++)
            {
                if (dteProjects.Item(i).Name == apiProjectName)
                    dteProject = dteProjects.Item(i);
            }

            string packageID = "EntityFramework";

            //Connect to the official package repository
            IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository("https://packages.nuget.org/api/v2");

            //PackageManager packageManager = new PackageManager(repo, path);
            var componentModel = (IComponentModel)ServiceProvider.GetService(typeof(SComponentModel));
            IVsPackageInstaller pckInstaller = componentModel.GetService<IVsPackageInstaller>();

            var packagesToInstall = new Dictionary<string, SemanticVersion>();

            var packages = new Dictionary<string, string>();

            packages.Add("EntityFramework", "EntityFramework");

            if (options["Unity"])
                packages.Add("Unity", "Unity.WebApi");

            if (options["CORS"])
                packages.Add("CORS", "Microsoft.AspNet.WebApi.Cors");


            foreach (var pkg in packages)
            {
                List<IPackage> package = repo.FindPackagesById(pkg.Value).ToList();
                var lastVersion = package.Where(o => o.IsLatestVersion).Select(o => o.Version).FirstOrDefault();

                packagesToInstall.Add(pkg.Value, lastVersion);
            }


            foreach (var pkg in packagesToInstall)
            {
                LogBox.AppendLine(GetHour() + " - Installing " + pkg.Key + " " + pkg.Value.Version);
                try
                {
                    pckInstaller.InstallPackage(_onlineNugetPackageLocation, dteProject, pkg.Key, pkg.Value.Version, false);
                    LogBox.AppendLine(GetHour() + " - " + pkg.Key + " " + pkg.Value.Version + " installed", Color.Green);
                }
                catch (Exception)
                {
                    LogBox.AppendLine(GetHour() + " - Error on installing " + pkg.Key + " " + pkg.Value.Version, Color.Red);
                }
            }

            //----------------------------------------------------------------------------------------------------------------
            // CHECK REFERENCES INTEGRITY
            LogBox.AppendLine(GetHour() + " - Checking references integrity of '" + apiProjectName + "'");
            GetCurrentSolution(out solution);
            apiProject = solution.Projects.First(o => o.Name == apiProjectName);
            var allReferences = apiProject.MetadataReferences;

            var refStatus = new Dictionary<string, bool> { { "EntityFramework", false } };

            foreach (var op in options)
            {
                if (op.Key == "Unity" && op.Value)
                    refStatus.Add("Unity", false);
                else if (op.Key == "CORS" && op.Value)
                    refStatus.Add("CORS", false);
            }

            foreach (var reference in allReferences)
            {
                if (reference.Display.Contains("EntityFramework"))
                    refStatus["EntityFramework"] = true;

                if (refStatus.ContainsKey("Unity"))
                {
                    if (reference.Display.Contains("Unity.WebApi"))
                        refStatus["Unity"] = true;
                }

                if (refStatus.ContainsKey("CORS"))
                {
                    if (reference.Display.Contains("System.Web.Http.Cors"))
                        refStatus["CORS"] = true;
                }
            }
            var allRequiredRefsOk = true;
            foreach (var rs in refStatus)
            {
                if (rs.Key == "EntityFramework")
                {
                    if (rs.Value)
                        LogBox.AppendLine(GetHour() + " - EntityFramework reference checked", Color.Green);
                    else
                    {
                        allRequiredRefsOk = false;
                        LogBox.AppendLine(GetHour() + " - EntityFramework reference not found, please, download it in NuGet Package manager", Color.Red);
                    }
                }
                if (rs.Key == "Unity")
                {
                    if (rs.Value)
                        LogBox.AppendLine(GetHour() + " - Unity.WebApi reference checked", Color.Green);
                    else
                    {
                        allRequiredRefsOk = false;
                        LogBox.AppendLine(GetHour() + " - Unity.WebApi reference not found, please, download it in NuGet Package manager", Color.Red);
                    }
                }
                if (rs.Key == "CORS")
                {
                    if (rs.Value)
                        LogBox.AppendLine(GetHour() + " - CORS reference checked", Color.Green);
                    else
                    {
                        allRequiredRefsOk = false;
                        LogBox.AppendLine(GetHour() + " - CORS reference not found, please, download it in NuGet Package manager (Microsoft.AspNet.WebApi.Cors)", Color.Red);
                    }
                }
            }


            //----------------------------------------------------------------------------------------------------------------
            // START TO GENERATE
            if (repositoryProject != null && apiProject != null && allRequiredRefsOk)
            {
                string dbContext = "";

                //----------------------------------------------------------------------------------------------------------------
                //CONNECTION STRINGS IMPORT
                try
                {
                    // only import if they are different projects
                    if (!sameProjects)
                    {
                        LogBox.AppendLine(GetHour() + " - Trying to import connection strings from '" +
                                          repositoryProjectName + "'");

                        // App.config file
                        var xmlRepDoc = new XmlDocument();
                        var apiRepPath =
                            Path.Combine(
                                solution.Projects.First(o => o.Name == repositoryProjectName)
                                    .FilePath.Replace(repositoryProjectName + ".csproj", ""), "App.Config");
                        xmlRepDoc.Load(apiRepPath);
                        var repConnectionStrings =
                            xmlRepDoc.DocumentElement.ChildNodes.Cast<XmlElement>()
                                .First(x => x.Name == "connectionStrings");

                        // if App.config contains connectionStrings element
                        if (repConnectionStrings != null)
                        {
                            var csdata = repConnectionStrings.ChildNodes.Cast<XmlElement>().First(x => x.Name == "add");

                            // Web.config file
                            var xmlApiDoc = new XmlDocument();
                            var apiApiPath =
                                Path.Combine(
                                    solution.Projects.First(o => o.Name == apiProjectName)
                                        .FilePath.Replace(apiProjectName + ".csproj", ""), "Web.config");
                            xmlApiDoc.Load(apiApiPath);
                            var apiConnectionStrings =
                                xmlApiDoc.DocumentElement.ChildNodes.Cast<XmlElement>()
                                    .FirstOrDefault(x => x.Name == "connectionStrings");

                            // DbContext getting
                            dbContext = csdata.GetAttribute("name");
                            LogBox.AppendLine(
                                GetHour() + " - Connection strings loaded with '" + dbContext + "' as DbContext ",
                                Color.Green);
                            LogBox.AppendLine(GetHour() + " - Trying to import connection strings  on '" +
                                              apiProjectName + "'");

                            var csnode = xmlApiDoc.ImportNode(repConnectionStrings, true);

                            // if API Web.config doesnt contain any connectionStrings element
                            if (apiConnectionStrings == null)
                            {
                                xmlApiDoc.DocumentElement.AppendChild(csnode);
                            }
                            // if Web.config alrealy contains that element
                            else
                            {
                                var addElement =
                                    apiConnectionStrings.ChildNodes.Cast<XmlElement>()
                                        .FirstOrDefault(x => x.Name == "add");
                                // if contains 'add' element
                                if (addElement != null)
                                {
                                    // if 'add' elements name  is the same as dbContext
                                    if (addElement.GetAttribute("name").Equals(dbContext))
                                        LogBox.AppendLine(
                                            GetHour() +
                                            " - API Web.Config file already contains a 'connectionStrings' element named '" +
                                            dbContext + "'", Color.Orange);
                                    else
                                    {
                                        apiConnectionStrings.AppendChild(xmlApiDoc.ImportNode(csdata, true));
                                    }
                                }
                                else
                                {
                                    apiConnectionStrings.AppendChild(xmlApiDoc.ImportNode(csdata, true));
                                }
                            }

                            xmlApiDoc.Save(apiApiPath);
                            LogBox.AppendLine(
                                GetHour() + " - Connection strings successfully imported to '" + apiProjectName + "'",
                                Color.Green);
                        }
                        else
                        {
                            LogBox.AppendLine(
                                GetHour() + " - Connection strings not found in App.config file of '" +
                                repositoryProjectName + "' project", Color.Red);
                        }
                    }
                    else
                    {
                        var xmlApiDoc = new XmlDocument();
                        var apiApiPath =
                            Path.Combine(
                                solution.Projects.First(o => o.Name == apiProjectName)
                                    .FilePath.Replace(apiProjectName + ".csproj", ""), "Web.config");
                        xmlApiDoc.Load(apiApiPath);
                        var apiConnectionStrings =
                            xmlApiDoc.DocumentElement.ChildNodes.Cast<XmlElement>()
                                .FirstOrDefault(x => x.Name == "connectionStrings");
                        var addElement = apiConnectionStrings.ChildNodes.Cast<XmlElement>().FirstOrDefault(x => x.Name == "add");
                        dbContext = addElement.GetAttribute("name");
                    }
                    ProgressBar.Value = 20;
                }
                catch (Exception)
                {
                    LogBox.AppendLine(GetHour() + " - Problems to import connection strings from '" + repositoryProjectName + "' to '" + apiProjectName + "' , make sure that Repository project contains App.Config file with 'connectionStrings' element", Color.Red);
                    ProgressBar.Value = 20;
                }


                CodeSnippets.ApiProjectName = apiProjectName;
                CodeSnippets.RepositoryProjectName = repositoryProjectName;

                //----------------------------------------------------------------------------------------------------------------
                // ADD REPOSITORY PROJECT REFERENCE
                if (!sameProjects)
                {
                    LogBox.AppendLine(GetHour() + " - Trying to reference '" + repositoryProjectName + "' on '" + apiProjectName + "'");

                    var alreadyReference = apiProject.ProjectReferences.Any(o => o.ProjectId == repositoryProject.Id);

                    if (!alreadyReference)
                    {
                        var projectWithReference = apiProject.AddProjectReference(new ProjectReference(repositoryProject.Id));
                        var res = workspace.TryApplyChanges(projectWithReference.Solution);
                        if (res)
                            LogBox.AppendLine(GetHour() + " - Reference added successfully", Color.Green);
                        else
                            LogBox.AppendLine(
                                GetHour() + " - Can't add the reference, you must add it manually after process end",
                                Color.Red);
                    }
                    else
                    {
                        LogBox.AppendLine(GetHour() + " - Reference was already added before process start", Color.Orange);
                    }
                }

                //----------------------------------------------------------------------------------------------------------------
                // GET REPOSITORY VIEWMODELS
                var viewModels = repositoryProject.Documents.Where(o => o.Folders.Contains("ViewModels") && o.Name != "IViewModel.cs");
                LogBox.AppendLine(GetHour() + " - Trying to get all ViewModels");
                if (viewModels.Any())
                {
                    LogBox.AppendLine(GetHour() + " - ViewModels loaded", Color.Green);
                    // save here all classnames to create the bootstraper file with unity registerType
                    var classesNameList = new List<string>();

                    // max number of PK from one properties
                    var maxPkSize = 1;

                    //Get the PK (name - type) of the current ViewModel and creates his controller
                    ProgressBar.Value = 50;

                    //----------------------------------------------------------------------------------------------------------------
                    // CONTROLLERS GENERATE
                    LogBox.AppendLine(GetHour() + " - Trying to generate all controllers");

                    // if BaseController is enabled
                    if (options["BaseController"])
                    {
                        var controllerName = "BaseController";
                        CodeSnippets.ControllerInheritance = controllerName;
                        GetCurrentSolution(out solution);
                        apiProject = solution.Projects.First(o => o.Name == apiProjectName);
                        var res = apiProject.AddDocument(controllerName, CodeSnippets.GetBaseController(controllerName), new[] { apiProjectName, "Controllers" });
                        workspace.TryApplyChanges(res.Project.Solution);
                    }

                    foreach (var vm in viewModels)
                    {
                        GetCurrentSolution(out solution);
                        apiProject = solution.Projects.First(o => o.Name == apiProjectName);

                        var data = await vm.GetSemanticModelAsync();
                        var currentClass = data.SyntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
                        var className = currentClass.Identifier.Text.Replace("ViewModel", "");

                        var methods = data.SyntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>();
                        var getKeysMethod = methods.First(o => o.Identifier.Text.Equals("GetKeys"));
                        var methodBody = getKeysMethod.Body.GetText().ToString();
                        var i1 = methodBody.IndexOf("{", methodBody.IndexOf("{") + 1) + 1;
                        var i2 = methodBody.IndexOf("}", methodBody.IndexOf("}"));
                        var pks = methodBody.Substring(i1, i2 - i1).Replace(" ", "").Split(',');

                        if (pks.Count() > maxPkSize)
                            maxPkSize = pks.Count();

                        var props = data.SyntaxTree.GetRoot().DescendantNodes().OfType<PropertyDeclarationSyntax>();
                        var primaryKeysList = new List<Tuple<string, string>>();
                        ProgressBar.Value = 50;
                        foreach (var p in props)
                        {
                            var pname = p.Identifier.Text;
                            var pline = p.GetText().ToString();
                            var pk = pks.FirstOrDefault(o => o.Equals(pname));
                            if (pk != null)
                            {
                                var ptype = pline.Substring(pline.IndexOf("public ") + 7, pline.IndexOf(" " + pk) - pline.IndexOf("public ") - 7);
                                primaryKeysList.Add(new Tuple<string, string>(pname, ptype));
                            }
                        }

                        // add controller
                        var res = apiProject.AddDocument(className + "Controller", CodeSnippets.GetRepositoryController(className, primaryKeysList, options["CORS"], options["Unity"], dbContext), new[] { apiProjectName, "Controllers" });

                        workspace.TryApplyChanges(res.Project.Solution);

                        classesNameList.Add(className);
                        LogBox.AppendLine(GetHour() + " - " + className + "Controller generated", Color.Green);
                    }
                    LogBox.AppendLine(GetHour() + " - All controllers generated successfully", Color.Green);
                    ProgressBar.Value = 60;

                    //----------------------------------------------------------------------------------------------------------------
                    // CREATE BOOTSTRAPPER FILE AND REGYSTERTYPES
                    if (options["Unity"] && bootstrapper)
                    {
                        LogBox.AppendLine(GetHour() + " - Trying to create Bootstrapper file");

                        GetCurrentSolution(out solution);
                        apiProject = solution.Projects.First(o => o.Name == apiProjectName);

                        var newFile = apiProject.AddDocument("Bootstrapper",
                            CodeSnippets.GetBootstrapper(classesNameList, dbContext),
                            new[] { apiProjectName, "App_Start" });
                        workspace.TryApplyChanges(newFile.Project.Solution);

                        GetCurrentSolution(out solution);
                        apiProject = solution.Projects.First(o => o.Name == apiProjectName);
                        LogBox.AppendLine(GetHour() + " - Bootstraper file created", Color.Green);
                        LogBox.AppendLine(GetHour() + " - Added all registerType statements for each entity", Color.Green);
                        // adds "Bootstrapper.InitUnity(container);" line in unity config
                        var unityConfigDoc =
                            apiProject.Documents.First(
                                o => o.Folders.Contains("App_Start") && o.Name == "UnityConfig.cs");
                        var tree = await unityConfigDoc.GetSyntaxTreeAsync();

                        var targetBlock =
                            tree.GetRoot()
                                .DescendantNodes()
                                .OfType<BlockSyntax>()
                                .FirstOrDefault(
                                    x =>
                                        x.Statements.Any(
                                            y => y.ToString().Contains("var container = new UnityContainer();")));

                        StatementSyntax syn1 =
                            SyntaxFactory.ParseStatement(@"
            Bootstrapper.InitUnity(container);

");
                        List<StatementSyntax> newSynList = new List<StatementSyntax> { syn1 };

                        SyntaxList<StatementSyntax> blockWithNewStatements = targetBlock.Statements;

                        foreach (var syn in newSynList)
                        {
                            blockWithNewStatements = blockWithNewStatements.Insert(1, syn);
                        }

                        BlockSyntax newBlock = SyntaxFactory.Block(blockWithNewStatements);

                        var newRoot = tree.GetRoot().ReplaceNode(targetBlock, newBlock);

                        var doc = unityConfigDoc.WithSyntaxRoot(newRoot);
                        workspace.TryApplyChanges(doc.Project.Solution);

                        ProgressBar.Value = 70;
                        GetCurrentSolution(out solution);
                        apiProject = solution.Projects.First(o => o.Name == apiProjectName);
                        LogBox.AppendLine(GetHour() + " - Added call to Bootstrapper in Web.Conf file", Color.Green);
                    }

                    //WEBAPI.CONFIG FILE
                    // adds unity init, json formatter and url mapping line in web config
                    LogBox.AppendLine(GetHour() + " - Trying to add configuration statements on WebApiConfig.cs");

                    var webApiConfigDoc = apiProject.Documents.First(o => o.Folders.Contains("App_Start") && o.Name == "WebApiConfig.cs");
                    var webApitree = await webApiConfigDoc.GetSyntaxTreeAsync();

                    var targetBlock2 =
                        webApitree.GetRoot()
                            .DescendantNodes()
                            .OfType<BlockSyntax>()
                            .FirstOrDefault(x => x.Statements.Any(y => y.ToString().Contains("config.MapHttpAttributeRoutes();")));

                    var stmt = "";

                    if (options["Unity"])
                    {
                        stmt += @"
            UnityConfig.RegisterComponents();
";
                        LogBox.AppendLine(GetHour() + " - Added component register of Unity", Color.Green);
                    }
                    if (options["JSON"])
                    {
                        stmt += @"
            var json = config.Formatters.JsonFormatter;
            json.SerializerSettings.PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects;
            config.Formatters.Remove(config.Formatters.XmlFormatter);
";
                        LogBox.AppendLine(GetHour() + " - Added JSON formatter", Color.Green);
                    }
                    if (options["CORS"])
                    {
                        stmt += @"
            config.EnableCors();

";
                        LogBox.AppendLine(GetHour() + " - Enabled CORS", Color.Green);
                    }

                    StatementSyntax syn2 = SyntaxFactory.ParseStatement(stmt);
                    var routeTemplate = "";
                    var defaults = "";
                    for (var i = 1; i <= maxPkSize; i++)
                    {
                        if (i == 1)
                        {
                            routeTemplate += "{id}";
                            defaults += "id = RouteParameter.Optional";
                            continue;
                        }
                        routeTemplate += "/{id" + i + "}";
                        defaults += ", id" + i + " = RouteParameter.Optional";
                    }
                    StatementSyntax syn3 =
                        SyntaxFactory.ParseStatement(@"
            config.Routes.MapHttpRoute(
                name: ""DefaultApi"",
                routeTemplate: ""api/{controller}/" + routeTemplate + @""",
                defaults: new { " + defaults + @" }
            );
");
                    List<StatementSyntax> newSynList2 = new List<StatementSyntax> { syn2 };

                    var r = targetBlock2.Statements.RemoveAt(1);

                    SyntaxList<StatementSyntax> blockWithNewStatements2 = r;

                    foreach (var syn in newSynList2)
                    {
                        blockWithNewStatements2 = blockWithNewStatements2.Insert(0, syn);
                    }
                    blockWithNewStatements2 = blockWithNewStatements2.Insert(blockWithNewStatements2.Count, syn3);

                    BlockSyntax newBlock2 = SyntaxFactory.Block(blockWithNewStatements2);

                    var newRoot2 = webApitree.GetRoot().ReplaceNode(targetBlock2, newBlock2);

                    var doc2 = webApiConfigDoc.WithSyntaxRoot(newRoot2);
                    workspace.TryApplyChanges(doc2.Project.Solution);
                    ProgressBar.Value = 100;
                }
                else
                {
                    LogBox.AppendLine(
                                GetHour() + " - ViewModels folder not found",
                                Color.Red);
                }
            }

            LogBox.AppendLine("---ACG process end---");
            ProgressBar.Value = 100;
            return true;
        }*/

        private void TwitterLink_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://twitter.com/franlsz95");
        }

        private void LinkedInLink_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://es.linkedin.com/in/francisco-lópez-sánchez-326907100");
        }

        private void GitHubLink_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/FranLsz/ApiControllerGenerator-VS-Extension");
        }

        private void GenerateFromRepositoryBtn_Click(object sender, EventArgs e)
        {
            Solution solution;
            GetCurrentSolution(out solution);
            if (solution.FilePath != null)
            {
                GeneratePanel.Visible = true;

                // add projects name to combobox
                foreach (var pr in solution.Projects)
                {
                    ApiProject.Items.Add(pr.Name);
                    RepositoryProject.Items.Add(pr.Name);
                }

                // if solution only have one project
                if (ApiProject.Items.Count == 1)
                {
                    ApiProject.SelectedIndex = 0;
                    RepositoryProject.SelectedIndex = 0;
                }
                else
                {
                    for (var i = 0; i < ApiProject.Items.Count; i++)
                    {
                        if (ApiProject.Items[i].ToString().ToLower().Contains("api") || !ApiProject.Items[i].ToString().ToLower().Contains("repository"))
                            ApiProject.SelectedIndex = i;
                        else if (ApiProject.Items[i].ToString().ToLower().Contains("repository") || !ApiProject.Items[i].ToString().ToLower().Contains("api"))
                            RepositoryProject.SelectedIndex = i;
                    }
                }
            }

            SolutionNotFoundLbl.Visible = true;
        }

        private void SettingsBtn_Click(object sender, EventArgs e)
        {
            SettingsBtn.Visible = false;
            SettingsPanel.Visible = true;
            //RepositoryTree.ExpandAll();
        }

        private void GoBackBtn_Click(object sender, EventArgs e)
        {
            SettingsBtn.Visible = true;
            SettingsPanel.Visible = false;
        }

        private void GenerateFromEF_Click(object sender, EventArgs e)
        {
            /*Solution solution;
            GetCurrentSolution(out solution);
            if (solution.FilePath != null)
                GeneratePanel.Visible = true;

            SolutionNotFoundLbl.Visible = true;*/
        }

        private void RepositoryIncluded_CheckedChanged(object sender, EventArgs e)
        {
            RepositoryProject.Enabled = !((CheckBox)sender).Checked;
        }

        private void ExitBtn_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
