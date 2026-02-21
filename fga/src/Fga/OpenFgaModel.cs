using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;
using OpenFga.Sdk.Model;

namespace ODataFga.Fga;

public static class OpenFgaSetup
{
    public static async Task<string> EnsureStoreAndModel(string apiUrl, string storeName)
    {
        OpenFgaClient client = new OpenFgaClient(new ClientConfiguration 
        { 
            ApiUrl = apiUrl 
        });

        // Check if the store "IntTestDemo" exists, if not create it
        ListStoresResponse listStoresResponse = await client.ListStores(new ClientListStoresRequest());

        string? storeId = listStoresResponse.Stores?.FirstOrDefault(s => s.Name == storeName)?.Id;
        
        if (string.IsNullOrEmpty(storeId)) 
        {
            ClientCreateStoreRequest request = new() { Name = storeName };

            CreateStoreResponse createStoreResponse = await client.CreateStore(request); 
            
            storeId = createStoreResponse.Id; 
        }

        client.StoreId = storeId;

        // Define the authorization model using the FgaModelBuilder
        List<TypeDefinition> typeDefinitions = new FgaModelBuilder()
            .Type("user").Type("group").Relation("member").Allow("user")

            .Type("folder")
                .Relation("viewer").Allow("user").Allow("group", "member")

            .Type("document")
                .Relation("parent").Allow("folder")
                .Relation("owner").Allow("user")
                .Relation("editor").Allow("user").OrRelation("owner")
                .Relation("viewer").Allow("user").OrRelation("editor").OrRelation("parent", "viewer")

                .Relation("can_share").OrRelation("owner").OrRelation("editor")
                .Relation("can_delete").OrRelation("owner")
                .Relation("can_move").OrRelation("owner").OrRelation("editor")
            .Build();

        ClientWriteAuthorizationModelRequest clientWriteAuthorizationRequest = new() 
        { 
            SchemaVersion = "1.1", 
            TypeDefinitions = typeDefinitions 
        };

        await client.WriteAuthorizationModel(clientWriteAuthorizationRequest);
        
        return storeId;
    }
}