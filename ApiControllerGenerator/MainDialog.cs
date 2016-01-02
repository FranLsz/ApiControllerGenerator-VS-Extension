using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;

namespace ApiControllerGenerator
{
    public partial class MainDialog : Form
    {
        private IServiceProvider ServiceProvider { get; set; }

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

        private async void GenerateBtn_Click(object sender, EventArgs e)
        {
            var workspace = GetWorkspace();
            Solution solution;
            GetCurrentSolution(out solution);

            if (solution.FilePath != null)
            {
                var apiProjectName = ProjectName.Text;
                var repositoryProjectName = "Repository";
                var repositoryProject = solution.Projects.First(o => o.Name == repositoryProjectName);
                var apiProject = solution.Projects.First(o => o.Name == apiProjectName);
                if (repositoryProject != null && apiProject != null)
                {
                    string dbContext = "";
                    try
                    {
                        //CONECCTION STRINGS IMPORT
                        var xmlRepDoc = new XmlDocument();
                        var apiRepPath = Path.Combine(solution.Projects.First(o => o.Name == repositoryProjectName).FilePath.Replace(repositoryProjectName + ".csproj", ""), "App.Config");
                        xmlRepDoc.Load(apiRepPath);
                        var repConnectionStrings = xmlRepDoc.DocumentElement.ChildNodes.Cast<XmlElement>().First(x => x.Name == "connectionStrings");
                        var csdata = repConnectionStrings.ChildNodes.Cast<XmlElement>().First(x => x.Name == "add");

                        var xmlApiDoc = new XmlDocument();
                        var apiApiPath = Path.Combine(solution.Projects.First(o => o.Name == apiProjectName).FilePath.Replace(apiProjectName + ".csproj", ""), "Web.config");
                        xmlApiDoc.Load(apiApiPath);

                        dbContext = csdata.GetAttribute("name");

                        var csnode = xmlApiDoc.ImportNode(repConnectionStrings, true);

                        xmlApiDoc.DocumentElement.AppendChild(csnode);
                        xmlApiDoc.Save(apiApiPath);
                    }
                    catch (Exception exception)
                    {
                        // ignore
                    }


                    CodeSnippets.ApiProjectName = apiProjectName;
                    try
                    {
                        var newProject = apiProject.AddProjectReference(new ProjectReference(repositoryProject.Id));
                        workspace.TryApplyChanges(newProject.Solution);
                    }
                    catch (Exception exception)
                    {
                        // ignore
                    }


                    // Gets all ViewModels
                    var viewModels = repositoryProject.Documents.Where(o => o.Folders.Contains("ViewModels") && o.Name != "IViewModel.cs");

                    if (viewModels.Any())
                    {
                        // saves here all classnames to create the bootstraper file with unity registerType
                        var classesNameList = new List<string>();
                        // max number of PK from one properties
                        var maxPkSize = 1;

                        //Gets the PK (name - type) of the current ViewModel and creates his controller
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

                            // adds controller
                            var res = apiProject.AddDocument(className + "Controller", CodeSnippets.GetRepositoryController(className, primaryKeysList), new[] { apiProjectName, "Controllers" });

                            workspace.TryApplyChanges(res.Project.Solution);

                            classesNameList.Add(className);
                        }
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




                    }
                    else
                    {
                        TestLabel.Text = "ViewModels not found";
                    }
                }
                else
                {
                    TestLabel.Text = "Repository or API project not found";
                }

            }
            else
            {
                TestLabel.Text = "Solution not found";
            }

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

        private void ProjectName_KeyUp(object sender, KeyEventArgs e)
        {
            GenerateBtn.Visible = ProjectName.Text.Trim().Length > 0;
        }

        private void ProjectName_KeyPress(object sender, KeyPressEventArgs e)
        {
            GenerateBtn.Visible = ProjectName.Text.Trim().Length > 0;
        }
    }
}
