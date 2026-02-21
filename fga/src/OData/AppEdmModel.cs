using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using ODataFga.Models;

namespace ODataFga.OData;


public static class AppEdmModel
{
    public static IEdmModel GetEdmModel()
    {
        ODataConventionModelBuilder b = new ODataConventionModelBuilder(); 
        
        b.EntitySet<Document>("Documents"); 
        b.EntitySet<Folder>("Folders"); 

        return b.GetEdmModel();
    }
}