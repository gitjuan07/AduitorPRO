# Controles Cruzados CC-001 a CC-012

Motor de reglas de negocio implementado en `MotorReglasService`. Cada control cruza datos de SAP, Nómina (EmpleadoMaestro) y Entra ID usando la **Cédula Normalizada** como llave maestra.

## Normalización de Cédula

```
NormalizarCedula(cedula) = cedula.Trim().Replace("-","").Replace(" ","").ToUpperInvariant()
```

Ejemplos: `"1-0234-5678"` → `"10234568"`, `" 10234568 "` → `"10234568"`, `"CR123"` → `"CR123"`

---

## CC-001 — Usuario SAP sin empleado activo en nómina

**Descripción:** Usuario SAP activo sin empleado correspondiente en el padrón de nómina (Evolution HR), cruzando por Cédula Normalizada.

**Semáforo:**
- Verde: 0 discrepancias
- Amarillo: 1–5 usuarios sin empleado
- Rojo: 6+ usuarios sin empleado

**Datos necesarios:** `UsuarioSistema` (SAP activos), `EmpleadoMaestro` (activos)

**Riesgo:** Accesos de personas que ya no están en plantilla, cuentas de servicio no catalogadas, o ex-empleados.

---

## CC-002 — Empleado inactivo/baja con usuario SAP activo

**Descripción:** Empleado con estado `INACTIVO` o `BAJA_PROCESADA` en nómina que sigue teniendo un usuario SAP habilitado.

**Semáforo:**
- Verde: 0 casos
- Amarillo: 1–2 casos
- Rojo: 3+ casos

**Datos necesarios:** `EmpleadoMaestro` (inactivos/baja), `UsuarioSistema` (SAP activos)

**Riesgo:** Alto — acceso no autorizado de ex-empleados a sistemas SAP.

---

## CC-003 — Usuario SAP sin actividad en 90+ días

**Descripción:** Usuario SAP activo que no ha iniciado sesión en los últimos 90 días.

**Semáforo:**
- Verde: 0 usuarios inactivos
- Amarillo: 1–10 usuarios
- Rojo: 11+ usuarios

**Datos necesarios:** `UsuarioSistema` (SAP, con `UltimoIngreso`)

**Acción recomendada:** Bloquear o deshabilitar — posibles cuentas huérfanas.

---

## CC-004 — Cuenta Entra ID habilitada sin EmployeeId

**Descripción:** Cuenta Azure AD (Entra ID) activa que no tiene el campo `EmployeeId` completado. El EmployeeId es la cédula que permite cruzar con SAP y Nómina.

**Semáforo:**
- Verde: 0 cuentas
- Amarillo: 1–3 cuentas
- Rojo: 4+ cuentas

**Datos necesarios:** Último `SnapshotEntraID` (registros con `AccountEnabled = true`)

**Acción recomendada:** Completar el perfil Entra ID con el EmployeeId (cédula).

---

## CC-005 — EmployeeId duplicado en Entra ID

**Descripción:** Dos o más cuentas en el snapshot Entra ID tienen el mismo `EmployeeId` normalizado. Solo puede existir un empleado por cédula.

**Semáforo:**
- Verde: 0 duplicados
- Rojo: cualquier duplicado (siempre rojo — no hay tolerancia)

**Datos necesarios:** Último `SnapshotEntraID`

**Acción recomendada:** Auditar las cuentas duplicadas — puede ser una cuenta personal vs corporativa, o un error de HR.

---

## CC-006 — Cédula SAP ≠ EmployeeId Entra ID (mismo email)

**Descripción:** Para usuarios que existen tanto en SAP como en Entra ID (cruzados por email/UPN), la cédula en SAP (`UsuarioSistema.Cedula` normalizada) no coincide con el `EmployeeIdNormalizado` en Entra ID.

**Semáforo:**
- Verde: 0 discrepancias
- Rojo: cualquier discrepancia (siempre rojo)

**Datos necesarios:** `UsuarioSistema` (SAP), `RegistroEntraID` (último snapshot)

**Riesgo:** Inconsistencia de identidad — impide los cruces de controles CC-001/002 y puede indicar cédula errónea en uno de los sistemas.

---

## CC-007 — Puesto/Sociedad SAP no está en la Matriz de Puestos

**Descripción:** Usuario SAP con combinación `Puesto + Sociedad` que no existe en la Matriz de Puestos aprobada por Contraloría.

**Semáforo:**
- Verde: 0 usuarios
- Amarillo: 1–5 usuarios
- Rojo: 6+ usuarios

**Datos necesarios:** `UsuarioSistema` (SAP), `MatrizPuestos`

**Acción recomendada:** Actualizar la Matriz de Puestos o corregir el perfil del usuario en SAP.

---

## CC-008 — Rol SAP no autorizado por la Matriz (sin excepción SE Suite)

**Descripción:** Usuario SAP tiene asignado un rol que no corresponde a su puesto/sociedad según la Matriz de Puestos, y no tiene una excepción vigente en SE Suite que lo justifique.

**Semáforo:**
- Verde: 0 asignaciones
- Amarillo: 1–3 asignaciones
- Rojo: 4+ asignaciones

**Datos necesarios:** `AsignacionRolUsuario`, `MatrizPuestos`, `CasoSeSuite`

**Lógica SE Suite:** Se verifica que exista un caso con `Estado = APROBADO`, `FechaVencimiento` futura, que incluya el rol del usuario y el mismo usuario SAP.

---

## CC-009 — Transacción SAP no autorizada por la Matriz (sin excepción SE Suite)

**Descripción:** Usuario SAP tiene acceso a una transacción que no está autorizada para su puesto/rol/sociedad en la Matriz, y no tiene excepción vigente en SE Suite.

**Semáforo:**
- Verde: 0 transacciones
- Amarillo: 1–5 transacciones
- Rojo: 6+ transacciones

**Datos necesarios:** `AsignacionRolUsuario` (con transacciones), `MatrizPuestos`, `CasoSeSuite`

**Lógica SE Suite:** Similar a CC-008 pero a nivel de transacción específica (tcode).

---

## CC-010 — Fecha de revisión de Contraloría vencida en la Matriz

**Descripción:** Registros en la Matriz de Puestos cuya `FechaRevisionContraloria` ya expiró, indicando que la autorización no ha sido renovada.

**Semáforo:**
- Verde: 0 registros vencidos
- Amarillo: 1–3 registros vencidos
- Rojo: 4+ registros vencidos

**Datos necesarios:** `MatrizPuestos` (con `FechaRevisionContraloria`)

**Acción recomendada:** Solicitar revisión y actualización de la Matriz a Contraloría.

---

## CC-011 — Rol crítico SAP sin expediente o SE Suite

**Descripción:** Usuario SAP con un rol marcado como crítico (`EsCritico = true`) que no tiene expediente documental ni caso SE Suite activo que lo respalde.

**Semáforo:**
- Verde: 0 usuarios
- Rojo: cualquier usuario (siempre rojo — tolerancia cero)

**Datos necesarios:** `AsignacionRolUsuario` (roles críticos), `CasoSeSuite`

**Acción recomendada:** Abrir un caso SE Suite o revocar el rol crítico.

---

## CC-012 — Conflicto de Segregación de Funciones (SoD)

**Descripción:** Usuario SAP que tiene asignados simultáneamente dos roles que crean un conflicto de SoD (Segregación de Funciones), según la tabla de conflictos `ConflictoSoD`.

**Semáforo:**
- Verde: 0 conflictos
- Amarillo: 1–2 conflictos
- Rojo: 3+ conflictos

**Datos necesarios:** `AsignacionRolUsuario`, `ConflictoSoD`

**Riesgo:** Fraude o error no detectado — un mismo usuario puede iniciar y aprobar una transacción crítica.

---

## Tabla resumen

| ID | Nombre corto | Fuentes de datos | Tolerancia | Dominio |
|---|---|---|---|---|
| CC-001 | SAP sin empleado nómina | SAP + EmpleadoMaestro | 0=V, 1-5=A, 6+=R | Identidad |
| CC-002 | Baja con SAP activo | EmpleadoMaestro + SAP | 0=V, 1-2=A, 3+=R | Identidad |
| CC-003 | SAP inactivo 90d | SAP (UltimoIngreso) | 0=V, 1-10=A, 11+=R | Accesos |
| CC-004 | Entra ID sin EmployeeId | SnapshotEntraID | 0=V, 1-3=A, 4+=R | Identidad |
| CC-005 | EmployeeId duplicado | SnapshotEntraID | 0=V, cualquier=R | Identidad |
| CC-006 | Cédula SAP ≠ Entra ID | SAP + SnapshotEntraID | 0=V, cualquier=R | Identidad |
| CC-007 | Puesto/Soc fuera de Matriz | SAP + MatrizPuestos | 0=V, 1-5=A, 6+=R | Accesos |
| CC-008 | Rol no autorizado Matriz | AsigRol + Matriz + SeSuite | 0=V, 1-3=A, 4+=R | Accesos |
| CC-009 | Tcode no autorizado Matriz | AsigRol + Matriz + SeSuite | 0=V, 1-5=A, 6+=R | Accesos |
| CC-010 | Revisión Contraloría vencida | MatrizPuestos | 0=V, 1-3=A, 4+=R | Gobernanza |
| CC-011 | Rol crítico sin expediente | AsigRol + SeSuite | 0=V, cualquier=R | Accesos |
| CC-012 | Conflicto SoD | AsigRol + ConflictoSoD | 0=V, 1-2=A, 3+=R | SoD |

**V** = Verde, **A** = Amarillo, **R** = Rojo
