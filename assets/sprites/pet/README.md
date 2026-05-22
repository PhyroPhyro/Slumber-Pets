# Pet Sprites

## Estrutura Esperada

### Placeholders (ColorRect)
Usados automaticamente pelo `PetSpriteAnimator` no modo `Placeholder`.
Cores definidas no código — não requerem arquivos.

### Textures (PNG)
Para usar no modo `Texture`, adicione PNGs na raiz desta pasta:

| Arquivo | Estado | Descrição |
|---|---|---|
| `idle.png` | Idle | Pet parado, neutro |
| `happy.png` | Happy | Pet feliz, animado |
| `sad.png` | Sad | Pet triste, cabisbaixo |
| `sleeping.png` | Sleeping | Pet dormindo |
| `eating.png` | Eating | Pet comendo |
| `playing.png` | Playing | Pet brincando |
| `walking.png` | Walking | Pet passeando |
| `traveling.png` | Traveling | Pet viajando |

### AnimatedSprite (SpriteFrames)
Para usar no modo `AnimatedSprite`, crie um `SpriteFrames` resource no Godot
com animações nomeadas em lowercase: `idle`, `happy`, `sad`, etc.

## Dimensões Recomendadas
- Tamanho base: 32x32 pixels (pixel art)
- Escala no jogo: 2x (64x64 visual)
- Formato: PNG com transparência

## Como Ativar
No código da cena, chame:
```csharp
var animator = GetNode<PetSpriteAnimator>("/root/PetSpriteAnimator");
animator.SetAnimationMode(PetSpriteAnimator.AnimationMode.Texture);
// ou
animator.SetAnimationMode(PetSpriteAnimator.AnimationMode.AnimatedSprite);
```
