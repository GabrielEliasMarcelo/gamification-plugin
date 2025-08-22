# 🎮 Azure DevOps Gamification Plugin

[🇺🇸 English](#english-version) | [🇧🇷 Português](#versão-em-português)

---

# English Version

## 📌 Overview
The **Azure DevOps Gamification Plugin** brings motivation and engagement to development teams by introducing **game mechanics** such as rankings, achievements, and real-time statistics directly into the Azure DevOps home dashboard.

It provides developers and managers with powerful insights, promoting collaboration, productivity, and healthy competition.

---

## 🚀 Features
- 📊 **Interactive Dashboard** with KPIs and rankings
- 🏆 **Developer Rankings** by commits (yearly, monthly, weekly)
- 🔍 **Code Quality Insights** with automated analysis
- 📈 **CodeGraph** similar to GitHub contribution graph
- 🎯 **Achievements & Badges** for milestones (e.g., 100 commits, first PR merged)
- 🔔 **Notifications** integrated with Azure DevOps

---

## 🏗️ Architecture

```mermaid
C4Context
      Person(dev, "Developer", "Commits code, pushes changes")
      System(ado, "Azure DevOps", "Main platform for development lifecycle")
      System_Ext(plugin, "Gamification Plugin", "Provides gamification layer inside Azure DevOps")
      dev -> ado : Uses
      ado -> plugin : Displays gamification data
```

```mermaid
C4Container
      System_Boundary(ado, "Azure DevOps Gamification Plugin") {
        Container(web, "Web Extension", "React + Tailwind", "Frontend UI inside Azure DevOps")
        Container(api, "Backend API", ".NET 8", "Processes requests, applies gamification logic")
        ContainerDb(db, "Database", "PostgreSQL/SQL Server", "Stores commits, metrics, achievements")
      }
      System_Ext(ado, "Azure DevOps")
      ado -> web : Hosts extension UI
      web -> api : REST API calls
      api -> db : Stores and retrieves gamification data
```

---

## ⚙️ Installation
1. Clone this repository  
   ```bash
   git clone https://github.com/your-org/azuredevops-gamification.git
   ```
2. Build and run the solution  
   ```bash
   dotnet build
   dotnet run --project src/Backend
   ```
3. Deploy the extension to Azure DevOps following Microsoft’s [official guide](https://learn.microsoft.com/en-us/azure/devops/extend/publish/overview).

---

## 📂 Project Structure
```
📦 azuredevops-gamification
 ┣ 📂 src
 ┃ ┣ 📂 Backend (.NET 8 API)
 ┃ ┗ 📂 Frontend (React + Tailwind)
 ┣ 📂 docs
 ┃ ┗ diagrams.md
 ┣ 📜 README.md
 ┣ 📜 manifest.json
 ┗ 📜 LICENSE
```

---

## 🗺️ Roadmap
- [x] Initial dashboard with rankings
- [x] CodeGraph integration
- [ ] Code quality analyzer integration (SonarQube / Azure DevOps API)
- [ ] Custom achievements system
- [ ] Slack/Teams integration for notifications

---

## 📜 License
MIT License © 2025

---

# Versão em Português

## 📌 Visão Geral
O **Azure DevOps Gamification Plugin** traz motivação e engajamento para equipes de desenvolvimento ao introduzir **mecânicas de jogo** como rankings, conquistas e estatísticas em tempo real diretamente no painel inicial do Azure DevOps.

Ele fornece insights poderosos para desenvolvedores e gestores, promovendo colaboração, produtividade e competição saudável.

---

## 🚀 Funcionalidades
- 📊 **Dashboard Interativo** com KPIs e rankings
- 🏆 **Ranking de Desenvolvedores** por commits (ano, mês, semana)
- 🔍 **Insights de Qualidade de Código** com análise automatizada
- 📈 **CodeGraph** similar ao gráfico de contribuições do GitHub
- 🎯 **Conquistas & Badges** para marcos (ex: 100 commits, primeiro PR aprovado)
- 🔔 **Notificações** integradas ao Azure DevOps

---

## 🏗️ Arquitetura

```mermaid
C4Context
      Person(dev, "Desenvolvedor", "Faz commits e push de alterações")
      System(ado, "Azure DevOps", "Plataforma principal do ciclo de desenvolvimento")
      System_Ext(plugin, "Plugin de Gamificação", "Fornece camada de gamificação dentro do Azure DevOps")
      dev -> ado : Usa
      ado -> plugin : Exibe dados de gamificação
```

```mermaid
C4Container
      System_Boundary(ado, "Azure DevOps Gamification Plugin") {
        Container(web, "Extensão Web", "React + Tailwind", "Interface dentro do Azure DevOps")
        Container(api, "API Backend", ".NET 8", "Processa requisições e aplica lógica de gamificação")
        ContainerDb(db, "Banco de Dados", "PostgreSQL/SQL Server", "Armazena commits, métricas, conquistas")
      }
      System_Ext(ado, "Azure DevOps")
      ado -> web : Hospeda UI da extensão
      web -> api : Chamadas REST API
      api -> db : Armazena e consulta dados de gamificação
```

---

## ⚙️ Instalação
1. Clone este repositório  
   ```bash
   git clone https://github.com/your-org/azuredevops-gamification.git
   ```
2. Compile e execute a solução  
   ```bash
   dotnet build
   dotnet run --project src/Backend
   ```
3. Publique a extensão no Azure DevOps seguindo o [guia oficial da Microsoft](https://learn.microsoft.com/pt-br/azure/devops/extend/publish/overview).

---

## 📂 Estrutura do Projeto
```
📦 azuredevops-gamification
 ┣ 📂 src
 ┃ ┣ 📂 Backend (.NET 8 API)
 ┃ ┗ 📂 Frontend (React + Tailwind)
 ┣ 📂 docs
 ┃ ┗ diagrams.md
 ┣ 📜 README.md
 ┣ 📜 manifest.json
 ┗ 📜 LICENSE
```

---

## 🗺️ Roadmap
- [x] Dashboard inicial com rankings
- [x] Integração com CodeGraph
- [ ] Integração com analisador de qualidade de código (SonarQube / Azure DevOps API)
- [ ] Sistema de conquistas personalizadas
- [ ] Integração com Slack/Teams para notificações

---

## 📜 Licença
Licença MIT © 2025
