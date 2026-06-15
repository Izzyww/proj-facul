# Foxy Food Rescue

**Foxy Food Rescue** é um jogo de plataforma 2D desenvolvido na Unity como projeto da disciplina **Game Development**. O jogador controla a **Foxy**, uma raposa que atravessa três fases para recuperar frutas roubadas por uma gangue de ladrões, enfrentando inimigos, armadilhas e um chefe final.

Repositório: https://github.com/Izzyww/proj-facul

---

## Descrição do jogo

Uma gangue de ladrões roubou as frutas da floresta. A Foxy parte em uma jornada para recuperá-las, passando por ambientes cada vez mais perigosos até enfrentar o líder da gangue: uma **águia** que ataca em dashes rápidos.

O jogo possui **três fases** com dificuldade progressiva:

| Fase | Ambiente | Conteúdo |
|------|----------|----------|
| **1 — Floresta** | Colinas, plataformas e props | Inimigos patrulhando, espinhos, cabeça mecânica que desce e sobe, coletáveis e checkpoint |
| **2 — Plataformas** | Buracos maiores e plataformas simétricas | Bola com corrente balançando, mais inimigos (no máximo um por trecho), saltos mais precisos |
| **3 — Chefe** | Arena no céu | Pulo duplo habilitado, plataformas em alturas diferentes, batalha contra a águia |

Antes da primeira fase, uma **tela introdutória** apresenta a história em texto (efeito typewriter). O jogador clica para começar.

**Objetivo:** coletar frutas para pontuar, sobreviver com **3 vidas** (ícones de cereja no HUD) e chegar ao **checkpoint** ao fim de cada fase. Na fase 3, derrote a águia acertando-a várias vezes enquanto ela descansa entre os ataques.

**Feedback visual e sonoro:** animações do personagem e inimigos, efeitos ao coletar frutas e derrotar inimigos, trilha sonora por fase e efeitos sonoros para pulo, dano, coleta e vitória.

---

## Mecânicas

### Movimentação e física
- Movimento horizontal com **Rigidbody2D** (física 2D, sem atrito no chão).
- Pulo com força aplicada por física; altura do pulo varia conforme o tempo que **Espaço** fica pressionado.
- Detecção de chão para impedir pulo infinito.
- **Shift** reduz a velocidade de corrida (caminhada lenta).
- **Pulo duplo** disponível apenas na fase do chefe.

### Combate e interação
- **Stomp:** pular em cima de um inimigo o derrota.
- **Roll:** rolar com **S** enquanto corre também elimina inimigos no caminho.
- Inimigos **patrulham** plataformas e viram ao encontrar parede ou borda (raycast).
- Contato lateral com inimigo, **espinhos** ou **armadilhas** causa dano, knockback e perda de uma vida.

### Wall-grab (agarrar na parede)
- No ar, segure **W** + direção **contra a parede** para agarrar.
- Solte a direção para cair.
- Pressione **Espaço** + direção oposta à parede para pular para longe.

### Coletáveis e progressão
- **Frutas** aumentam a pontuação e exibem efeito visual ao serem coletadas.
- **Checkpoint** no fim de cada fase avança para a próxima.
- **Queda no vazio** remove uma vida e reinicia a posição do jogador.

### Obstáculos
- **Espinhos** fixos no chão.
- **Cabeça mecânica** que desce e sobe automaticamente em intervalos.
- **Bola com corrente** balançando entre plataformas.

### Chefe (águia)
- Aparece de um lado da arena e **acompanha a altura do jogador** por alguns segundos (telegraph).
- Em seguida, executa um **dash em linha reta** na altura travada.
- Após o ataque, **descansa** e fica vulnerável — pule em cima para causar dano.
- Vários acertos são necessários para vencer; barra de vida do chefe aparece no HUD.

### Interface (HUD)
- Pontuação total de coletáveis.
- Indicador de vidas (cerejas).
- Barra de vida do chefe (fase 3).

---

## Controles

| Ação | Tecla |
|------|--------|
| Mover para esquerda / direita | `A` / `D` ou `←` / `→` |
| Andar devagar | `Shift` + movimento |
| Pular | `Espaço` |
| Pulo variável (mais alto = segurar mais) | Manter `Espaço` pressionado |
| Pulo duplo | `Espaço` no ar (apenas fase 3) |
| Olhar para cima | `W` ou `↑` |
| Rolar (derrota inimigos) | `S` ou `↓` enquanto corre |
| Agarrar na parede | `W` + direção contra a parede (no ar) |
| Pular da parede | `Espaço` + direção oposta à parede |
| Continuar na tela introdutória | Clique do mouse ou `Espaço` |

---

## Instruções de execução

### Requisitos

- **Unity Hub** com **Unity 6000.4.11f1**
- **Windows 10/11** (para executável `.exe`)
- Módulo **Windows Build Support** no Unity Hub
- Git (opcional, para clonar o repositório)

### Opção A — Rodar no Unity Editor

1. Clone o repositório (ou baixe e extraia o ZIP):

```bash
git clone https://github.com/Izzyww/proj-facul.git
cd proj-facul
```

2. Abra o **Unity Hub** → **Open** → selecione a pasta `proj-facul`.
3. Use a versão **6000.4.11f1** e aguarde a importação dos pacotes (primeira abertura pode demorar).
4. Abra a cena `Assets/Scenes/SampleScene.unity`.
5. **Obrigatório na primeira vez:** menu **Tools → Level → Construir Nivel (Auto-Setup)** — gera prefabs e conecta inimigos, frutas, música e HUD ao gerador de fases.
6. Salve a cena (**Ctrl + S**) e clique em **Play**.

### Opção B — Rodar o executável Windows

1. Gere o build (veja abaixo) ou use um `.zip` já entregue com a pasta `FoxyFoodRescue`.
2. Extraia o conteúdo (se estiver compactado).
3. Execute **`FoxyFoodRescue.exe`**.
4. Mantenha a pasta **`FoxyFoodRescue_Data`** e as DLLs no mesmo diretório do `.exe`.

### Opção C — Gerar o executável (build)

**Pelo Editor (recomendado):**

1. Feche outras instâncias da Unity com este projeto aberto.
2. **File → Build Settings**
3. Confirme `SampleScene` em **Scenes In Build**.
4. Plataforma **Windows**, arquitetura **x86_64** → **Build** (ou **Build And Run**).
5. Salve em `Builds/FoxyFoodRescue/FoxyFoodRescue.exe`.
6. Para distribuir: compacte a pasta inteira (`FoxyFoodRescue.exe` + `FoxyFoodRescue_Data` + DLLs).

**Por linha de comando (opcional):**

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe" `
  -quit -batchmode -nographics `
  -projectPath "C:\caminho\para\proj-facul" `
  -buildWindows64Player "C:\caminho\para\proj-facul\Builds\FoxyFoodRescue\FoxyFoodRescue.exe" `
  -logFile "Builds\unity-build.log"
```

Substitua `C:\caminho\para\proj-facul` pelo caminho real. A Unity Editor **não** pode estar aberta no mesmo projeto durante o build em batch mode.

---

## Estrutura do projeto

| Pasta / arquivo | Descrição |
|-----------------|-----------|
| `Assets/Scripts/` | Lógica do jogo (player, fases, chefe, áudio, HUD) |
| `Assets/Editor/LevelBuilder.cs` | Auto-setup de prefabs |
| `Assets/_Generated/` | Prefabs gerados pelo LevelBuilder |
| `Assets/Scenes/SampleScene.unity` | Cena jogável |
| `Builds/` | Saída do executável (ignorada pelo Git) |

Scripts principais: `PlayerController`, `LevelGenerator`, `GameManager`, `EnemyPatrol`, `EagleBoss`, `AudioManager`, `IntroScreen`.

---

## Créditos de assets

- **Pixel Adventure** — tiles, frutas, traps, efeitos visuais
- **Sunny Land** — personagem Foxy, inimigos, props, backgrounds
- **Sunny Land Music** — trilhas sonoras (`.ogg`)
- **TextMesh Pro** — textos da interface

Código C# desenvolvido para este projeto.

---

## Problemas comuns

| Problema | Solução |
|----------|---------|
| Personagem não se move | **Edit → Project Settings → Player → Active Input Handling:** Both. Confirme layer **Ground** no chão. |
| Sem inimigos, frutas ou música | Rode **Tools → Level → Construir Nivel (Auto-Setup)** e salve a cena. |
| Build falha (“another Unity instance”) | Feche a Unity Editor deste projeto. |
| Pasta `Library/` ausente no clone | Normal — a Unity recria ao abrir o projeto. |

---

## Autor

**Ismael Pereira Netto** — 95636  
Disciplina: **Game Development**

Projeto desenvolvido para fins acadêmicos. Assets de terceiros permanecem sujeitos às licenças dos pacotes originais.
