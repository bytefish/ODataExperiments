using OpenFga.Sdk.Model;

namespace ODataFga.Fga;

public class FgaModelBuilder
{
    private readonly List<TypeDefinition> _types = new();

    public FgaTypeBuilder Type(string t)
    {
        var d = new TypeDefinition
        {
            Type = t,
            Relations = new(),
            Metadata = new() { Relations = new() }
        };

        _types.Add(d);

        return new FgaTypeBuilder(this, d);
    }

    public List<TypeDefinition> Build() => _types;
}

public class FgaTypeBuilder
{
    private readonly FgaModelBuilder _p;
    private readonly TypeDefinition _t;

    public FgaTypeBuilder(FgaModelBuilder p, TypeDefinition t)
    {
        _p = p;
        _t = t;
    }

    public FgaRelationBuilder Relation(string r) => new FgaRelationBuilder(this, _t, r);

    public FgaTypeBuilder Type(string t) => _p.Type(t);

    public List<TypeDefinition> Build() => _p.Build();
}

public class FgaRelationBuilder
{
    private readonly FgaTypeBuilder _p;
    private readonly TypeDefinition _t;
    private readonly string _rn;

    private readonly List<Userset> _u = new();

    public FgaRelationBuilder(FgaTypeBuilder p, TypeDefinition t, string r)
    {
        _p = p;
        _t = t;
        _rn = r;
    }

    public FgaRelationBuilder Allow(string t)
    {
        _u.Add(new Userset { This = new object() });

        UpdateMeta(t);

        return this;
    }

    public FgaRelationBuilder Allow(string t, string r)
    {
        _u.Add(new Userset
        {
            TupleToUserset = new TupleToUserset
            {
                Tupleset = new ObjectRelation { Relation = r },
                ComputedUserset = new ObjectRelation { Relation = _rn }
            }
        });

        UpdateMeta(t);

        return this;
    }

    public FgaRelationBuilder OrRelation(string r, string? c = null)
    {
        if (c == null)
        {
            _u.Add(new Userset { ComputedUserset = new ObjectRelation { Relation = r } });
        }
        else
        {
            _u.Add(new Userset { TupleToUserset = new TupleToUserset { Tupleset = new ObjectRelation { Relation = r }, ComputedUserset = new ObjectRelation { Relation = c } } });
        }

        return this;
    }

    private void UpdateMeta(string t)
    {
        if(_t.Metadata?.Relations == null)
        {
            _t.Metadata = new Metadata { Relations = new() };
        }

        if (!_t.Metadata.Relations.ContainsKey(_rn))
        {
            _t.Metadata.Relations[_rn] = new RelationMetadata { DirectlyRelatedUserTypes = new() };
        }
        
        _t.Metadata.Relations[_rn].DirectlyRelatedUserTypes!.Add(new RelationReference { Type = t });
    }

    private void Commit() { 
        
        if(_t.Relations == null)
        {
            _t.Relations = new();
        }

        if (_u.Count == 1)
        {
            _t.Relations[_rn] = _u[0];
        }
        else
        {
            _t.Relations[_rn] = new Userset { Union = new Usersets { Child = _u } };
        } 
    }

    public FgaRelationBuilder Relation(string n) 
    { 
        Commit(); 
        return _p.Relation(n); 
    }

    public FgaTypeBuilder Type(string n) 
    { 
        Commit(); 
        return _p.Type(n); 
    }

    public List<TypeDefinition> Build() 
    { 
        Commit(); 
        return _p.Build(); 
    }
}