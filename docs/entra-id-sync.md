# Sincronización Entra ID via Microsoft Graph

## Descripción

AuditorPRO TI puede sincronizar el directorio Azure Active Directory (Entra ID) directamente mediante la API de Microsoft Graph, sin necesidad de exportar archivos Excel manualmente.

Cada ejecución crea un `SnapshotEntraID` con `Origen = "GRAPH_DIRECT"` y recupera todos los usuarios del tenant con paginación automática.

## Endpoint

```
POST /api/cargas/snapshot-entraid/sync
Content-Type: application/json

{
  "nombreInstantanea": "Auditoría Q2 2026"   // opcional
}
```

**Respuesta exitosa (200):**
```json
{
  "snapshotId": "3fa85f64-...",
  "nombre": "Entra ID (Graph) — 23/04/2026 21:44",
  "fechaInstantanea": "2026-04-23T21:44:12Z",
  "totalRegistros": 1247,
  "errores": 0,
  "detalleErrores": [],
  "origen": "GRAPH_DIRECT"
}
```

**Respuesta de error Graph (502):**
```json
{
  "errors": ["Error Graph (403): Insufficient privileges to complete the operation."]
}
```

## Arquitectura

```
Frontend (Cargas.tsx)
  └─ POST /api/cargas/snapshot-entraid/sync
       └─ SyncEntraIDDirectoCommand (MediatR)
            └─ IEntraIdSyncService
                 └─ EntraIdSyncService
                      ├─ DefaultAzureCredential → GraphServiceClient
                      ├─ Paginación Graph (top=999, OdataNextLink)
                      ├─ IdentityNormalizationHelper.NormalizarCedula()
                      └─ AppDbContext → SnapshotEntraID + RegistroEntraID[]
```

## Campos recuperados de Graph

| Campo Graph | Campo DB |
|---|---|
| `id` | `ObjectId` |
| `displayName` | `DisplayName` |
| `userPrincipalName` | `UserPrincipalName` |
| `mail` | `Email` (fallback a UPN) |
| `department` | `Department` |
| `jobTitle` | `JobTitle` |
| `accountEnabled` | `AccountEnabled` |
| `employeeId` | `EmployeeId` + `EmployeeIdNormalizado` |
| `officeLocation` | `OfficeLocation` |
| `createdDateTime` | `CreatedDateTime` |
| `signInActivity.lastSignInDateTime` | `LastSignInDateTime` |

`EmployeeIdNormalizado` = trim + remove hyphens + uppercase (cédula normalizada para cruce con SAP y Nómina).

## Autenticación

En **Azure** (producción): la `Managed Identity` asignada al App Service/Container App necesita el permiso de aplicación `User.Read.All` en Microsoft Graph.

En **desarrollo local** (Codespace): usa `az login` — la `DefaultAzureCredential` selecciona automáticamente las credenciales del CLI.

### Configurar permisos (Azure Portal)

1. **Azure Portal** → **Entra ID** → **App registrations** → busca la Managed Identity por nombre
2. **API permissions** → **Add a permission** → **Microsoft Graph** → **Application permissions**
3. Agregar: `User.Read.All`
4. **Grant admin consent** para el tenant

### Configurar permisos (PowerShell / Graph)

```powershell
# Obtener el Service Principal de la Managed Identity
$sp = Get-MgServicePrincipal -Filter "displayName eq 'z-aud-app-prod'"

# Obtener el Service Principal de Microsoft Graph
$graph = Get-MgServicePrincipal -Filter "appId eq '00000003-0000-0000-c000-000000000000'"

# Obtener el rol User.Read.All
$role = $graph.AppRoles | Where-Object { $_.Value -eq 'User.Read.All' }

# Asignar el permiso
New-MgServicePrincipalAppRoleAssignment `
  -ServicePrincipalId $sp.Id `
  -PrincipalId $sp.Id `
  -ResourceId $graph.Id `
  -AppRoleId $role.Id
```

## Variables de entorno / Configuración

No se requiere configuración adicional para el sync directo cuando la Managed Identity está correctamente asignada. La `DefaultAzureCredential` usa la identidad del runtime automáticamente.

Para deshabilitar la autenticación interactiva (ya configurado):
```csharp
new DefaultAzureCredentialOptions { ExcludeInteractiveBrowserCredential = true }
```

## Troubleshooting

| Síntoma | Causa probable | Solución |
|---|---|---|
| `Error Graph (403)` | Sin permiso `User.Read.All` | Asignar permiso y hacer grant admin consent |
| `Identity not found` | Managed Identity no asignada al recurso | Asignar System/User Managed Identity en el App Service |
| `Graph no devolvió resultados` | Permisos insuficientes o tenant vacío | Verificar permisos y tenant ID |
| Sync lento (>2 min) | Tenant con >10,000 usuarios | Normal — hay paginación automática |
| `signInActivity` nulo | Permiso `AuditLog.Read.All` faltante | Agregar ese permiso si se necesita LastSignIn |

## Comparativa: Graph Direct vs Excel Manual

| | Graph Direct | Excel Manual |
|---|---|---|
| **Frecuencia** | Tiempo real / on-demand | Periódico (manual) |
| **Esfuerzo** | Un clic | Exportar → limpiar → subir |
| **Cobertura** | 100% del tenant | Lo que está en el Excel |
| **EmployeeId** | Directo del perfil Entra ID | Depende del Excel |
| **Origen** | `GRAPH_DIRECT` | `MANUAL_EXCEL` |
| **Requiere** | Managed Identity con `User.Read.All` | Solo acceso a la UI |
