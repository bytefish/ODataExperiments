using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;
using OpenFga.Sdk.Model;

namespace ODataFga.Fga;

public static class OpenFgaSetup
{
    public static async Task<string> EnsureStoreAndModel(string apiUrl)
    {
        OpenFgaClient client = new OpenFgaClient(new ClientConfiguration { ApiUrl = apiUrl });

        ListStoresResponse listStoresResponse = await client.ListStores(new ClientListStoresRequest());

        string? storeId = listStoresResponse.Stores?.FirstOrDefault(s => s.Name == "IntTestDemo")?.Id;

        if (string.IsNullOrEmpty(storeId))
        {
            CreateStoreResponse createStoreResponse = await client.CreateStore(new ClientCreateStoreRequest { Name = "IntTestDemo" });

            storeId = createStoreResponse.Id;
        }

        client.StoreId = storeId;

        List<TypeDefinition> typeDefinitions = new FgaModelBuilder()
            .Type("user").Type("group").Relation("member").Allow("user")
            .Type("folder").Relation("viewer").Allow("user").Allow("group", "member")
            .Type("document").Relation("parent").Allow("folder").Relation("owner").Allow("user")
            .Relation("editor").Allow("user").OrRelation("owner")
            .Relation("viewer").Allow("user").OrRelation("editor").OrRelation("parent", "viewer")
            .Relation("approver").Allow("user")
            .Build();

        await client.WriteAuthorizationModel(new ClientWriteAuthorizationModelRequest { SchemaVersion = "1.1", TypeDefinitions = typeDefinitions });

        return storeId;
    }
}