### Vape Scape - The Smoking Maze Game
##### Download and Play - [Itch.io](https://inglorious-ratbastard.itch.io/vape-scape-early-release)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![DOTNET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![VSCode](https://img.shields.io/badge/VSCode-0078D4?style=for-the-badge&logo=visual%20studio%20code&logoColor=white)
![Itch.io](https://img.shields.io/badge/Itch.io-FA5C5C?style=for-the-badge&logo=itchdotio&logoColor=white)<br>

##### **Vape Scape** is a first-person atmospheric maze exploration game built with **WPF (.NET 10)** and a custom software raycasting engine. Players navigate procedurally generated mazes while managing vape resources that function as both a light source and a survival mechanic.

---

### Features

* ##### Custom Raycasting Engine
* ##### Real-time first-person rendering
* ##### Dynamic wall shading
* ##### Distance-based lighting
* ##### Fog and visibility effects

#### Procedural Maze Generation
* ##### Randomly generated mazes every level
* ##### Increasing maze complexity as levels progress
* ##### Intelligent exit placement using path-distance calculations

#### Vape Survival System

##### - Your vape device serves as your primary source of visibility.

##### Manage three critical resources:

* ##### **Battery** – powers the vape light
* ##### **Liquid** – required to produce vapor
* ##### **Coil Condition** – affects vape performance

##### - Poor resource management can leave you trapped in darkness.

#### Dynamic Fog System
* ##### Volumetric fog simulation
* ##### Deployable vape smoke clouds
* ##### Visibility reduction mechanics
* ##### Atmospheric environmental effects

#### Progressive Difficulty
##### Maze sizes increase as players advance:

| Level Range | Maze Size |
| ----------- | --------- |
| 1 - 5       | 13 x 13   |
| 6 - 15      | 15 x 15   |
| 16 - 25     | 21 x 21   |
| 26 - 35     | 25 x 25   |
| 36 - 45     | 29 x 29   |
| 46+         | 33 x 33   |

#### Map System
* ##### Limited-use map charges
* ##### Earn additional charges at milestone levels
* ##### Reveals nearby maze structures
* ##### Displays player orientation and nearby exits

#### Leaderboard System
##### * Local JSON-based persistence
##### * Tracks:
  * ##### Player name
  * ##### Highest level reached
  * ##### Mazes completed
  * ##### Achievement date

#### Visual Effects
* ##### Animated splash screen
* ##### Menu smoke particle system
* ##### Dynamic vape glow effects
* ##### Exit beacon lighting
* ##### Vignette post-processing
* ##### Atmospheric fog rendering

---

### Controls
| Key      | Action                |
| -------- | --------------------- |
| ↑        | Move Forward          |
| ↓        | Move Backward         |
| ←        | Turn Left             |
| →        | Turn Right            |
| Space    | Activate Vape Light   |
| Shift    | Release Smoke Cloud   |
| Ctrl + P | Use Map Charge        |
| C        | Toggle Controls Panel |
---

### Gameplay

#### Objective
##### Navigate through increasingly difficult mazes and locate the exit beacon to advance to the next level.

#### Survival
##### Every use of the vape affects:
* ##### Battery
* ##### Liquid
* ##### Coil durability
  
##### - Players must carefully balance visibility and resource consumption.

#### Winning
##### Reach **Level 50** and escape the final maze to complete the game.

---

### Technical Details

#### Built With
* ##### C#
* ##### WPF
* ##### .NET 10
* ##### System.Text.Json

#### Core Systems
* ##### Custom Raycasting Renderer
* ##### Recursive Backtracking Maze Generation
* ##### Breadth-First Search Exit Placement
* ##### Particle Effects Engine
* ##### Fog Density Sampling
* ##### Persistent Leaderboard Storage

#### Data Storage
##### Leaderboard entries are stored locally in:

```text
leaderboard.json
```

##### Example entry:

```json
{
  "Name": "Player1",
  "Level": 35,
  "MazesCompleted": 34,
  "DateAchieved": "2026-01-01T12:00:00"
}
```

---

### Project Structure

```text
MainWindow.xaml
MainWindow.xaml.cs

Assets
 └── Vape Image

Data
 └── leaderboard.json
```

---

### Future Improvements

* ##### Sound effects and ambient audio
* ##### Additional maze themes
* ##### Save/load game functionality
* ##### Enemy encounters
* ##### Achievement system
* ##### Global online leaderboards
* ##### Controller support
* ##### Enhanced visual effects

---

### Credits

#### Development

##### **Lead Developer:** Javier Yzaguirre

#### Game Concept

##### **Taylor Watson**

---

### License

##### © 2026 Vape Scape. All rights reserved.
