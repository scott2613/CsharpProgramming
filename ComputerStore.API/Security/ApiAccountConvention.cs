using ComputerStore.Application.DTOs.Accounts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;

namespace ComputerStore.API.Security;
// Mặc định API quản lý yêu cầu Admin; chỉ mở GET sản phẩm/danh mục công khai.
public sealed class ApiAccountConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            if (!controller.Attributes.OfType<ApiControllerAttribute>().Any() || controller.ControllerName is "Users" or "Orders") continue;
            controller.Filters.Add(new AuthorizeFilter(new IAuthorizeData[] { new AuthorizeAttribute { Roles = StoreRoles.Admin } }));
            controller.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            if (controller.ControllerName is not ("Product" or "Products" or "ProductsApi" or "Categories")) continue;
            foreach (var action in controller.Actions)
                if (action.Attributes.OfType<HttpMethodAttribute>().Any(a => a.HttpMethods.Contains("GET")))
                    action.Filters.Add(new AllowAnonymousFilter());
        }
    }
}
