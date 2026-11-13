# Modelo de dados

```
ApplicationUser (Identity) 1─1 UserProfile
                           1─1 ContactPrivacy
ApplicationUser ─< EventMembership >─ Event          (RoleInEvent, Status)
Event ─< Company ─< CompanyMembership >─ ApplicationUser
Event ─< Post ─< Comment
Post ─< Reaction (PK: PostId + UserId)
Report (EventId, ReporterId, TargetType, TargetId, Status, ReviewedById)
AuditLog (ActorId, Action, TargetType, TargetId, EventId?)
```

| Entidade | Campos principais |
| --- | --- |
| ApplicationUser | Id (Guid), Email (único), PasswordHash, DisplayName, IsActive, CreatedAt |
| UserProfile | UserId, PhotoUrl, CompanyName, JobTitle, Area, Bio, Interests (text[]), LookingFor, Offers |
| ContactPrivacy | UserId, EmailVisible, PhoneVisible, Phone, ConsentGivenAt |
| Event | Id, Name, Description, StartsAt, EndsAt, Location, Status (Draft/Active/Finished/Archived) |
| EventMembership | EventId + UserId, RoleInEvent (Participant/Exhibitor/Organizer), Status (Active/Disabled/Removed) |
| Company | Id, EventId, Name, Description, Segment, ProductsServices, BoothLocation, Website |
| CompanyMembership | CompanyId + UserId, Title |
| Post / Comment | Texto, autor, data, Status (Visible/Hidden/Deleted); Post tem Category e EventId |
| Reaction | PostId + UserId (impede duplicidade), Type (Like/Insightful/Celebrate) |
| Report | Alvo (Post/Comment/Profile), motivo, Status (Open/Dismissed/ActionTaken) |

**Decisões / desvios do documento técnico** (a confirmar no MER):

- `DisplayName` fica em `ApplicationUser` (dado básico de Identity), não em `UserProfile`.
- `Report` ganhou `EventId` para que cada organizador modere só o seu evento (RN07).
- `AuditLog` foi adicionada para o requisito de registrar ações administrativas.
- Interesses são normalizados em minúsculas para permitir filtro exato por interesse.
- Datas usam `DateTimeOffset` em UTC.
