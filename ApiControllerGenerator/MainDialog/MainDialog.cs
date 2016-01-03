using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using ApiControllerGenerator.CodeSnippet;
using ApiControllerGenerator.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;

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

        // GENERATE BUTTON
        private async void GenerateBtn_Click(object sender, EventArgs e)
        {
            GeneratePanel.Visible = false;
            ProcessPanel.Visible = true;

            var apiProjectName = ApiProject.SelectedItem.ToString();
            var repositoryProjectName = RepositoryIncluded.Checked ? apiProjectName : RepositoryProject.SelectedItem.ToString();

            var options = new Dictionary<string, bool>
            {
                {"BaseController", GenerateOptions_BaseController.Checked},
                {"Unity", GenerateOptions_Unity.Checked},
                {"CORS", GenerateOptions_CORS.Checked},
                {"JSON", GenerateOptions_JSON.Checked}
            };

            var r = await AcgGenerateProcess(apiProjectName, repositoryProjectName, options);

            if (r)
                ExitBtn.Visible = true;
        }

        // ACG GENERATE PROCESS
        private async Task<bool> AcgGenerateProcess(string apiProjectName, string repositoryProjectName, Dictionary<string, bool> options)
        {
            var sameProjects = repositoryProjectName == apiProjectName;

            LogBox.AppendLine("---ACG process start---");

            // WORKSPACE GETTING
            LogBox.AppendLine(GetHour() + " - Trying to get workspace");
            var workspace = GetWorkspace();
            LogBox.AppendLine(GetHour() + " - Workspace loaded", Color.Green);
            ProgressBar.Value = 5;

            // SOLUTION GETTING
            LogBox.AppendLine(GetHour() + " - Trying to get solution");
            Solution solution;
            GetCurrentSolution(out solution);
            LogBox.AppendLine(GetHour() + " - Current solution loaded", Color.Green);
            ProgressBar.Value = 10;

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

            // CHECK REFERENCES INTEGRITY
            LogBox.AppendLine(GetHour() + " - Checking references integrity of '" + apiProjectName + "'");
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


            // START TO GENERATE
            if (repositoryProject != null && apiProject != null && allRequiredRefsOk)
            {
                string dbContext = "";
                //CONNECTION STRINGS IMPORT
                try
                {
                    // only import if they are different projects
                    if (!sameProjects)
                    {
                        LogBox.AppendLine(GetHour() + " - Trying to import connection strings from '" + repositoryProjectName + "'");

                        // App.config file
                        var xmlRepDoc = new XmlDocument();
                        var apiRepPath = Path.Combine(solution.Projects.First(o => o.Name == repositoryProjectName).FilePath.Replace(repositoryProjectName + ".csproj", ""), "App.Config");
                        xmlRepDoc.Load(apiRepPath);
                        var repConnectionStrings = xmlRepDoc.DocumentElement.ChildNodes.Cast<XmlElement>().First(x => x.Name == "connectionStrings");

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
                            LogBox.AppendLine(GetHour() + " - Trying to import connection strings  on '" + apiProjectName + "'");

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
                                            " - API Web.config file already contains a 'connectionStrings' element named '" +
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
                            LogBox.AppendLine(GetHour() + " - Connection strings not found in App.config file of '" + repositoryProjectName + "' project", Color.Red);
                        }
                    }
                    ProgressBar.Value = 20;
                }
                catch (Exception)
                {
                    LogBox.AppendLine(GetHour() + " - Problems to import connection strings from '" + repositoryProjectName + "' to '" + apiProjectName + "' , make sure that Repository project contains App.config file with connectionStrings element", Color.Red);
                    ProgressBar.Value = 20;
                }


                CodeSnippets.ApiProjectName = apiProjectName;
                CodeSnippets.RepositoryProjectName = repositoryProjectName;

                // ADD REPOSITORY PROJECT REFERENCE
                if (!sameProjects)
                {
                    LogBox.AppendLine(GetHour() + " - Trying to reference '" + repositoryProjectName + "' in '" + apiProjectName + "'");

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

                // CONTROLLERS GENERATE
                // get all view models
                var viewModels = repositoryProject.Documents.Where(o => o.Folders.Contains("ViewModels") && o.Name != "IViewModel.cs");

                if (viewModels.Any())
                {
                    // save here all classnames to create the bootstraper file with unity registerType
                    var classesNameList = new List<string>();
                    // max number of PK from one properties
                    var maxPkSize = 1;

                    //Get the PK (name - type) of the current ViewModel and creates his controller
                    ProgressBar.Value = 40;

                    foreach (var vm in viewModels)
                    {
                        LogBox.AppendLine(GetHour() + " - Workspace loaded", Color.Green);
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
                            LogBox.AppendLine(GetHour() + " - Workspace loaded", Color.Green);
                            var pname = p.Identifier.Text;
                            var pline = p.GetText().ToString();
                            var pk = pks.FirstOrDefault(o => o.Equals(pname));
                            if (pk != null)
                            {
                                var ptype = pline.Substring(pline.IndexOf("public ") + 7, pline.IndexOf(" " + pk) - pline.IndexOf("public ") - 7);
                                primaryKeysList.Add(new Tuple<string, string>(pname, ptype));
                            }
                        }

                        // adds controller
                        var res = apiProject.AddDocument(className + "Controller", CodeSnippets.GetRepositoryController(className, primaryKeysList), new[] { apiProjectName, "Controllers" });

                        workspace.TryApplyChanges(res.Project.Solution);

                        classesNameList.Add(className);
                    }
                    ProgressBar.Value = 60;
                    LogBox.AppendLine(GetHour() + " - Workspace loaded", Color.Green);
                    GetCurrentSolution(out solution);
                    apiProject = solution.Projects.First(o => o.Name == apiProjectName);

                    // creates Bootstrapper file
                    var newFile = apiProject.AddDocument("Bootstrapper", CodeSnippets.GetBootstrapper(classesNameList, dbContext), new[] { apiProjectName, "App_Start" });
                    workspace.TryApplyChanges(newFile.Project.Solution);

                    GetCurrentSolution(out solution);
                    apiProject = solution.Projects.First(o => o.Name == apiProjectName);

                    // adds "Bootstrapper.InitUnity(container);" line in unity config
                    var unityConfigDoc = apiProject.Documents.First(o => o.Folders.Contains("App_Start") && o.Name == "UnityConfig.cs");
                    var tree = await unityConfigDoc.GetSyntaxTreeAsync();

                    var targetBlock =
                        tree.GetRoot()
                            .DescendantNodes()
                            .OfType<BlockSyntax>()
                            .FirstOrDefault(x => x.Statements.Any(y => y.ToString().Contains("var container = new UnityContainer();")));

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
                    LogBox.AppendLine(GetHour() + " - Workspace loaded", Color.Green);
                    GetCurrentSolution(out solution);
                    apiProject = solution.Projects.First(o => o.Name == apiProjectName);
                    // adds unity init, json formatter and url mapping line in web config
                    var webApiConfigDoc = apiProject.Documents.First(o => o.Folders.Contains("App_Start") && o.Name == "WebApiConfig.cs");
                    var webApitree = await webApiConfigDoc.GetSyntaxTreeAsync();

                    var targetBlock2 =
                        webApitree.GetRoot()
                            .DescendantNodes()
                            .OfType<BlockSyntax>()
                            .FirstOrDefault(x => x.Statements.Any(y => y.ToString().Contains("config.MapHttpAttributeRoutes();")));

                    StatementSyntax syn2 =
                        SyntaxFactory.ParseStatement(@"
            UnityConfig.RegisterComponents();

            var json = config.Formatters.JsonFormatter;
            json.SerializerSettings.PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects;
            config.Formatters.Remove(config.Formatters.XmlFormatter);

");
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
                    LogBox.AppendLine(GetHour() + " - Workspace loaded", Color.Green);
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
        }

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
            // SettingsPanel.Visible = true;
            //RepositoryTree.ExpandAll();
        }

        private void GoBackBtn_Click(object sender, EventArgs e)
        {
            SettingsBtn.Visible = true;
            //SettingsPanel.Visible = false;
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
