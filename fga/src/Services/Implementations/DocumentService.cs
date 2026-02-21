using Microsoft.EntityFrameworkCore;
using ODataFga.Database;
using ODataFga.Dtos;
using ODataFga.Fga;
using ODataFga.Models;
using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;
using System.Transactions;

namespace ODataFga.Services.Implementations;

public class DocumentService : SecuredResourceService<Document, Folder>, IDocumentService
{
    public DocumentService(AppDbContext db, IOpenFgaClient fga, ICurrentUserService user, ILogger<DocumentService> logger)
        : base(db, fga, user, logger) { }

    public async Task<Document> CreateDocumentAsync(CreateDocumentRequest request) => await CreateAsync(new Document { Title = request.Title, ParentId = request.ParentId });
    public async Task MoveDocumentAsync(string key, MoveDocumentRequest request) => await MoveAsync(key, request.NewParentId);
    public async Task DeleteDocumentAsync(string key) => await DeleteAsync(key);
    public async Task ShareDocumentAsync(string key, string targetUserId, string relation) => await ShareAsync(key, targetUserId, relation);
}