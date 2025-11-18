# SSRS RDL Compatibility Fix - DataSource Type Issue

## Problem
Error: "'Type' attribute is not allowed. Line 4, position 41."

This occurs when SSRS tries to deserialize the RDL file and encounters an empty or invalid `Type` attribute in the `DataSource` element.

## Root Cause
In `RDLBuilder.cs`, the `CreateDataSources()` method was setting:
```xml
<DataSource Name="OracleDataSource" Type="">
```

The `Type` attribute is being set to empty string, which SSRS RDL parser doesn't accept.

## Solution
Remove the `Type` attribute entirely from the `DataSource` element. SSRS uses the `DataProvider` in `ConnectionProperties` to determine the data source type.

### Correct RDL Structure
```xml
<DataSource Name="OracleDataSource">
    <ConnectionProperties>
        <DataProvider>ORACLE</DataProvider>
        <ConnectString>...</ConnectString>
        <IntegratedSecurity>false</IntegratedSecurity>
    </ConnectionProperties>
</DataSource>
```

## Fix Applied
In `RDLBuilder.cs` - `CreateDataSources()` method:
- ❌ REMOVED: `dataSource.SetAttribute("Type", "");`
- ✅ KEPT: All ConnectionProperties elements
- ✅ KEPT: DataSource Name attribute

## Verification
1. Build project
2. Convert RPT to RDL
3. Try to deserialize/open in SSRS
4. Should NOT show "Type attribute" error

## SSRS Supported Data Providers
- SQLSERVER
- ORACLE
- OLEDB
- ODBC
- XML
- MOSS
- SHAREPOINT
- SAPBW
- SAPWEBSERVICES

The `DataProvider` element (inside `ConnectionProperties`) tells SSRS which provider to use, NOT the `Type` attribute.
