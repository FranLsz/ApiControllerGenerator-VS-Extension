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
            System.Diagnostics.Process.Start("https://github.com/FranLsz");
        }

        private async void GenerateBtn_Click(object sender, EventArgs e)
        {
            var workspace = GetWorkspace();
            Solution solution;
            GetCurrentSolution(out solution);

            if (solution.FilePath != null)
            {
                var apiProjectName = "RPGTestProject";
                var repositoryProjectName = "Repository";
                var repositoryProject = solution.Projects.First(o => o.Name == repositoryProjectName);
                var apiProject = solution.Projects.First(o => o.Name == apiProjectName);
                if (repositoryProject != null && apiProject != null)
                {

                    var xmlRepDoc = new XmlDocument();
                    var apiRepPath = Path.Combine(solution.Projects.First(o => o.Name == repositoryProjectName).FilePath.Replace(repositoryProjectName + ".csproj", ""), "App.Config");
                    xmlRepDoc.Load(apiRepPath);
                    var repConnectionStrings = xmlRepDoc.DocumentElement.ChildNodes.Cast<XmlElement>().First(x => x.Name == "connectionStrings");
                    var csdata = repConnectionStrings.ChildNodes.Cast<XmlElement>().First(x => x.Name == "add");
                    var xmlApiDoc = new XmlDocument();
                    var apiApiPath = Path.Combine(solution.Projects.First(o => o.Name == ProjectName.Text).FilePath.Replace(ProjectName.Text + ".csproj", ""), "Web.Config");
                    xmlApiDoc.Load(apiApiPath);
                    try
                    {
                        var nameAttr = xmlApiDoc.CreateAttribute("name");
                        var connectionStringAttr = xmlApiDoc.CreateAttribute("connectionString");
                        var providerNameAttr = xmlApiDoc.CreateAttribute("name");

                        nameAttr.Value = csdata.GetAttribute("name");
                        connectionStringAttr.Value = csdata.GetAttribute("connectionString");
                        providerNameAttr.Value = csdata.GetAttribute("providerName");

                        var nodeCs = xmlApiDoc.CreateElement("connectionStrings");
                        var node = xmlApiDoc.CreateElement("add");

                        node.Attributes.Append(nameAttr);
                        node.Attributes.Append(connectionStringAttr);
                        node.Attributes.Append(providerNameAttr);
                        nodeCs.AppendChild(node);

                        var csnode = xmlApiDoc.ImportNode(repConnectionStrings, true);

                        var m = xmlApiDoc.DocumentElement.AppendChild(csnode);
                    }
                    catch (Exception exception)
                    {
                        // ignore
                    }


                    CodeSnippets.ApiProjectName = ProjectName.Text;
                    try
                    {
                        var newProject = apiProject.AddProjectReference(new ProjectReference(repositoryProject.Id));
                        workspace.TryApplyChanges(newProject.Solution);
                    }
                    catch (Exception exception)
                    {
                        // ignore
                    }




                    var viewModels = repositoryProject.Documents.Where(o => o.Folders.Contains("ViewModel") && o.Name != "IViewModel.cs");

                    if (viewModels.Any())
                    {
                        var classesNameList = new List<string>();
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


                            var res = apiProject.AddDocument(className + "Controller", CodeSnippets.GetRepositoryController(className, pks), new[] { apiProjectName, "Controllers" });

                            workspace.TryApplyChanges(res.Project.Solution);

                            classesNameList.Add(className);
                        }
                        GetCurrentSolution(out solution);
                        apiProject = solution.Projects.First(o => o.Name == apiProjectName);
                        var newFile = apiProject.AddDocument("Bootstrapper", CodeSnippets.GetBootstrapper(classesNameList, "BLABLABLA"), new[] { apiProjectName, "App_Start" });
                        workspace.TryApplyChanges(newFile.Project.Solution);

                        GetCurrentSolution(out solution);
                        apiProject = solution.Projects.First(o => o.Name == apiProjectName);

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
