using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ApiControllerGenerator
{
    public class CodeSnippets
    {
        public static string ApiProjectName = "ApiProject";
        public static string RepositoryProjectName = "Repository";
        public static string RepositoryModelsFolderName = "Models";
        public static string ControllerInheritance = "ApiController";

        public static string GetRepositoryController(string className, string[] primaryKeys)
        {
            var code = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Practices.Unity;
using " + RepositoryProjectName + @"." + RepositoryModelsFolderName + @";
using " + RepositoryProjectName + @".Repository;
using " + RepositoryProjectName + @".ViewModel;

namespace " + ApiProjectName + @".Controllers
{
    public class " + className + @"Controller : " + ControllerInheritance + @"
    {
        [Dependency]
        public IRepository<" + className + @", " + className + @"ViewModel> " + className + @"Repository { get; set; }

        //GET
        [ResponseType(typeof(" + className + @"ViewModel))]
        public IHttpActionResult Get()
        {
            return Ok(" + className + @"Repository.Get());
        }

        //GET BY ID
        [ResponseType(typeof(" + className + @"ViewModel))]
        public IHttpActionResult Get([FromUri]int id)
        {
            var data = " + className + @"Repository.Get(id);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        //POST
        [ResponseType(typeof(" + className + @"ViewModel))]
        public IHttpActionResult Post([FromBody] " + className + @"ViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

        var resModel = " + className + @"Repository.Add(model);
            return Created(""DefaultApi"", resModel);
        }

        //PUT
        [ResponseType(typeof(" + className + @"ViewModel))]
        public IHttpActionResult Put([FromUri]int id, [FromBody] " + className + @"ViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            if (id != model." + primaryKeys[0] + @")
            {
                return BadRequest();
            }

            if (" + className + @"Repository.Get(id) == null)
                return NotFound();

            var rows = " + className + @"Repository.Update(model);
            return Ok(rows);
        }

        //DELETE
        [ResponseType(typeof(" + className + @"ViewModel))]
        public IHttpActionResult Delete([FromUri]int id)
        {
            var model = " + className + @"Repository.Get(id);

            if (model == null)
                return NotFound();

            var rows = " + className + @"Repository.Delete(model);
            return Ok(rows);
        }
    }
}";
            return code;
        }

        public static string GetBootstrapper(List<string> classes, string entityDbContext)
        {
            var types = classes.Aggregate("", (current, c) => current + $"\n            container.RegisterType<IRepository<{c}, {c}ViewModel>, EntityRepository<{c}, {c}ViewModel>>();");
            var code = @"
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using Microsoft.Practices.Unity;
using " + RepositoryProjectName + @"." + RepositoryModelsFolderName + @";
using " + RepositoryProjectName + @".Repository;
using " + RepositoryProjectName + @".ViewModel;

namespace " + ApiProjectName + @"
{
    public class Bootstrapper
    {
        public static void InitUnity(UnityContainer container)
        {
            container.RegisterType<DbContext, " + entityDbContext + @">();
" + types + @"
        }
    }
}";
            return code;
        }
    }
}
