# Foxy Food Rescue

Jogo de plataforma 2D feito na Unity para projeto acadêmico. Você controla a Foxy, uma raposa que atravessa três fases para recuperar frutas roubadas por uma gangue de ladrões, enfrentando inimigos, armadilhas e um chefe final (águia).

Repositório: https://github.com/Izzyww/proj-facul

---

## Requisitos

- **Unity Hub** com **Unity 6000.4.11f1**
- **Windows 10/11** (para build e execução do `.exe`)
- Módulo **Windows Build Support** instalado no Unity Hub
- Git (para clonar o repositório)

---

## Como abrir o projeto

1. Clone o repositório:

```bash
git clone https://github.com/Izzyww/proj-facul.git
cd proj-facul
```

2. Abra o **Unity Hub** → **Open** → selecione a pasta `proj-facul`.
3. Use a versão **6000.4.11f1** quando o Hub pedir.
4. Aguarde a Unity importar os pacotes na primeira abertura (pode demorar alguns minutos).

A cena principal já está configurada: `Assets/Scenes/SampleScene.unity`.

---

## Configuração antes de jogar (obrigatório na primeira vez)

Depois de abrir o projeto, rode o auto-setup para gerar/atualizar prefabs e conectar tudo no `LevelGenerator`:

1. No menu da Unity: **Tools → Level → Construir Nivel (Auto-Setup)**
2. Salve a cena: **Ctrl + S**
3. Pressione **Play**

Se pular esse passo, alguns prefabs (inimigos, frutas, música, HUD) podem não estar ligados na cena.

---

## Como jogar (controles)

| Ação | Tecla |
|------|--------|
| Mover | `A` / `D` ou setas |
| Andar devagar | `Shift` + movimento |
| Pular | `Espaço` |
| Olhar para cima | `W` |
| Rolar (mata inimigos) | `S` correndo |
| Agarrar na parede | `W` + direção contra a parede (no ar) |
| Pular da parede | `Espaço` + direção oposta à parede |

- **3 vidas** (cerejas no canto superior esquerdo)
- **Pontuação** ao coletar frutas
- **Fase 3:** pulo duplo habilitado; chefe exige esquivar e pular em cima quando ele descansa

---

## Rodar no Editor

1. Abra `Assets/Scenes/SampleScene.unity`
2. Execute **Tools → Level → Construir Nivel (Auto-Setup)** (se ainda não rodou)
3. Clique em **Play**

---

## Como gerar o executável (Windows)

### Pelo Editor (recomendado)

1. Feche qualquer outra instância da Unity com este projeto aberto.
2. **File → Build Settings**
3. Confirme que `SampleScene` está na lista **Scenes In Build**
4. Plataforma: **Windows** → **Switch Platform** (se necessário)
5. **Architecture:** x86_64
6. Clique em **Build** (ou **Build And Run**)
7. Escolha uma pasta de saída, por exemplo:

```
Builds/FoxyFoodRescue/FoxyFoodRescue.exe
```

8. Para entregar compactado: zipar a pasta inteira `FoxyFoodRescue` (`.exe` + `FoxyFoodRescue_Data` + DLLs).

### Por linha de comando (opcional)

Com a Unity **fechada** para este projeto:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe" `
  -quit -batchmode -nographics `
  -projectPath "C:\caminho\para\proj-facul" `
  -buildWindows64Player "C:\caminho\para\proj-facul\Builds\FoxyFoodRescue\FoxyFoodRescue.exe" `
  -logFile "Builds\unity-build.log"
```

Substitua `C:\caminho\para\proj-facul` pelo caminho real da pasta clonada.

**Importante:** não deixe o projeto aberto no Editor ao rodar build em batch mode — a Unity bloqueia duas instâncias no mesmo projeto.

---

## Estrutura do projeto (resumo)

| Pasta / arquivo | Descrição |
|-----------------|-----------|
| `Assets/Scripts/` | Lógica do jogo (player, fases, chefe, áudio, HUD) |
| `Assets/Editor/LevelBuilder.cs` | Auto-setup de prefabs e wiring |
| `Assets/_Generated/` | Prefabs gerados pelo LevelBuilder |
| `Assets/Scenes/SampleScene.unity` | Cena jogável |
| `Builds/` | Saída do executável (ignorada pelo Git) |

Scripts principais: `PlayerController`, `LevelGenerator`, `GameManager`, `EnemyPatrol`, `EagleBoss`, `AudioManager`, `IntroScreen`.

---

## Créditos de assets

- **Pixel Adventure** — tiles, frutas, traps, efeitos
- **Sunny Land** — personagem Foxy, inimigos, props, backgrounds e trilhas (`.ogg` em `Assets/SunnyLand Music/`)

Código C# desenvolvido para este projeto.

---

## Problemas comuns

| Problema | O que fazer |
|----------|-------------|
| Personagem não se move | **Edit → Project Settings → Player → Active Input Handling:** Both (Old + New). Confirme que o chão está na layer **Ground**. |
| Sem inimigos / frutas / música | Rode **Tools → Level → Construir Nivel (Auto-Setup)** e salve a cena. |
| Build batch falha (“another Unity instance”) | Feche a Unity Editor deste projeto e tente de novo. |
| Pasta `Library/` não está no Git | Normal — a Unity recria ao abrir o projeto. |

---

## Licença / uso acadêmico

Projeto desenvolvido para disciplina de Game Development. Assets de terceiros permanecem sujeitos às licenças dos pacotes originais.
