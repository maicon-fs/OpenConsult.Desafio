# LDAP Console Application

Este é um projeto simples de console em C# que interage com um servidor LDAP (OpenLDAP) com base nas configurações de um arquivo `appsettings.json` e nas entradas de arquivos XML.

## Funcionalidades

- Conecta-se a um servidor LDAP com base nas configurações definidas no `appsettings.json`.
- Utiliza um arquivo `docker-compose.yml` para criar o container com o servidor OpenLDAP.
- Realiza operações no LDAP a partir de arquivos XML.
- Usa `IConfiguration` para carregar configurações de um arquivo JSON.
- Utiliza `XPath` para consultar dados nos arquivos XML.
- Utiliza o pacote `Novell.Directory.Ldap.NETStandard` para interagir com servidores LDAP (OpenLDAP).

## Versão .NET necessária

- **.NET 8** ou superior.

## Pacotes NuGet utilizados

- `Microsoft.Extensions.Configuration`
- `Microsoft.Extensions.Configuration.Json`
- `Microsoft.Extensions.Configuration.Binder`
- `Microsoft.Extensions.FileProviders.Physical`
- `Novell.Directory.Ldap.NETStandard`

## Configuração

### Arquivo `appsettings.json`

O arquivo de configuração `appsettings.json` deve estar presente na raiz do projeto, contendo as seguintes informações:

```json
{
  "LdapSettings": {
    "Host": "localhost",
    "Port": 389,
    "AdminPassword": "1212",
    "BaseDn": "dc=desafio,dc=ldap",
    "AdminUser": "cn=admin,dc=desafio,dc=ldap",
    "UsersContainer": "ou=users,dc=desafio,dc=ldap",
    "GroupsContainer": "ou=groups,dc=desafio,dc=ldap"
  }
}
```
Esse arquivo define as configurações para se conectar ao servidor LDAP, como o endereço do host, a porta de conexão, credenciais de administrador, e os containers de usuários e grupos no LDAP.

## Como Executar

### 1. Clone o repositório:

```bash
git clone https://github.com/maicon-fs/OpenConsult.Desafio.git
```

### 2. Navegue até a pasta da solução:

```bash
cd OpenConsult.Desafio
```

### 3. Inicie o container Docker com o OpenLDAP
```bash
docker-compose up -d
```

### 4. Navegue até a pasta do projeto

```bash
cd OpenConsult.Desafio.XmlProcessor
```

### 5. Restaure as dependências:

```bash
dotnet restore
```

### 6. Execute o projeto:

```bash
dotnet run
```
