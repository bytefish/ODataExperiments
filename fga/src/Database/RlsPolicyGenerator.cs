namespace ODataFga.Database;

/// <summary>
/// Provides methods for generating SQL statements to configure row-level security policies on database tables.
/// </summary>
/// <remarks>This class enables the creation and management of fine-grained access control policies for SELECT,
/// INSERT, UPDATE, and DELETE operations. The generated SQL ensures that only authorized users can access or modify
/// data based on defined permissions. Use this class to dynamically update security policies as application
/// requirements change.</remarks>
public static class RlsPolicyGenerator
{
    /// <summary>
    /// Generates SQL statements to enable row-level security and create policies for the specified table and FGA type.
    /// </summary>
    /// <param name="tableName">Table to apply the RLS Policy for</param>
    /// <param name="fgaType">FGA Type we need to secure</param>
    /// <returns>The Postgres RLS Policy</returns>
    public static string Generate(string tableName, string fgaType)
    {
        return $@"
            ALTER TABLE ""{tableName}"" ENABLE ROW LEVEL SECURITY;
            
            DROP POLICY IF EXISTS rls_{tableName}_select ON ""{tableName}"";
            CREATE POLICY rls_{tableName}_select ON ""{tableName}"" FOR SELECT USING (
                NULLIF(current_setting('app.current_user', true), '') IS NOT NULL AND (
                    -- Direct access check
                    EXISTS (
                        SELECT 1 FROM ""Permissions"" p
                        WHERE p.""ObjectType"" = '{fgaType}'
                          AND p.""ObjectId"" = ""{tableName}"".""Id""
                          AND p.""UserId"" = current_setting('app.current_user', true)
                          AND (p.""PermissionMask"" & cast(NULLIF(current_setting('app.required_mask', true), '') as integer)) = cast(NULLIF(current_setting('app.required_mask', true), '') as integer)
                    ) 
                    OR 
                    -- Inherited access check (Recursive/Hierarchy)
                    EXISTS (
                        SELECT 1 FROM ""Permissions"" p
                        WHERE p.""UserId"" = current_setting('app.current_user', true)
                          AND (p.""PermissionMask"" & cast(NULLIF(current_setting('app.required_mask', true), '') as integer)) = cast(NULLIF(current_setting('app.required_mask', true), '') as integer)
                          AND p.""ObjectId"" = ANY(""{tableName}"".""AncestorIds"")
                    )
                )
            );

            DROP POLICY IF EXISTS rls_{tableName}_insert ON ""{tableName}"";
            CREATE POLICY rls_{tableName}_insert ON ""{tableName}"" FOR INSERT WITH CHECK (true);
            DROP POLICY IF EXISTS rls_{tableName}_update ON ""{tableName}"";
            CREATE POLICY rls_{tableName}_update ON ""{tableName}"" FOR UPDATE USING (true);
            DROP POLICY IF EXISTS rls_{tableName}_delete ON ""{tableName}"";
            CREATE POLICY rls_{tableName}_delete ON ""{tableName}"" FOR DELETE USING (true);
        ";
    }
}
