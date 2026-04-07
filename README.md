# Chinese Chess Engine Using Unity

A Xiangqi (Chinese Chess) project built in **Unity**, with supporting **Python tools** for dataset creation and preprocessing.

This repository contains two main parts:

- **Unity game / engine code** for the Chinese Chess project, including gameplay logic, UI flow, experiment setup, and piece spawning. :contentReference[oaicite:0]{index=0} :contentReference[oaicite:1]{index=1} :contentReference[oaicite:2]{index=2} :contentReference[oaicite:3]{index=3}
- **Python scripts** for dataset generation, parsing game records, labeling positions with Pikafish, and building value-network training datasets. :contentReference[oaicite:4]{index=4}

---

## Features

### Unity side
- Xiangqi board and piece management
- Main game controller for move flow, turn handling, replay recording, undo, restart, and AI turns. :contentReference[oaicite:5]{index=5}
- Experiment setup system with selectable experiment counts such as 1, 5, and 10 rounds. :contentReference[oaicite:6]{index=6}
- Persistent game settings between scenes, including selected side, AI side, mode, and experiment count. :contentReference[oaicite:7]{index=7}
- Piece spawning system for initial Xiangqi positions. :contentReference[oaicite:8]{index=8}

### Python side
- Parsing Xiangqi game records from text files
- Building board tensors for training
- Generating and labeling positions using **Pikafish**
- Combining human and pro game data into training datasets
- Saving datasets in `.npz` format for model training. :contentReference[oaicite:9]{index=9}

---

## Project Structure

```text
MyProject/
├─ Assets/                  # Unity assets, scripts, prefabs, scenes
├─ Packages/                # Unity package manifest
├─ ProjectSettings/         # Unity project settings
├─ python/                  # Python dataset / preprocessing scripts
├─ .gitignore
└─ README.md
```
## Acknowledgements

Special thanks to the following projects and contributors whose resources and work were helpful references for this project:

- **BOYOFANS** — for providing Xiangqi game records used as a reference for dataset collection and analysis.  
  [OnlineXiangqi dataset on Kaggle](https://www.kaggle.com/datasets/boyofans/onlinexiangqi?resource=download)

- **Code Monkey / Wukong Xiangqi** — for reference on the mailbox board structure and professional Chinese Chess game records.  
  [Wukong Xiangqi GitHub repository](https://github.com/maksimKorzh/wukong-xiangqi)

- **Pikafish** — for supporting the creation of evaluation and policy datasets through engine-based position analysis and move generation.  
  [Pikafish GitHub repository](https://github.com/official-pikafish/Pikafish)
  ## Acknowledgements

This project was developed with reference to several helpful resources, datasets, and artworks. Special thanks to:

- **BOYOFANS** for providing Xiangqi game records that were useful for dataset reference and analysis.  
  [OnlineXiangqi dataset on Kaggle](https://www.kaggle.com/datasets/boyofans/onlinexiangqi?resource=download)

- **Code Monkey / Wukong Xiangqi** for reference material on the mailbox board representation and professional Chinese Chess game records.  
  [Wukong Xiangqi GitHub repository](https://github.com/maksimKorzh/wukong-xiangqi)

- **Pikafish** for enabling evaluation and policy dataset creation through engine analysis and move generation.  
  [Pikafish GitHub repository](https://github.com/official-pikafish/Pikafish)

- **Wikipedia user Wj654cj86** for providing the Xiangqi board image used as a visual reference in this project.  
  [Xiangqi board SVG on Wikimedia Commons](https://commons.wikimedia.org/wiki/File:Xiangqi_board.svg)

- **Xiangqi.com** for providing the Chinese Chess piece artwork used in the project.  
  [Xiangqi.com graphics](https://www.xiangqi.com/graphics)

- **Google Gemini** for creating the background and button art used in the project UI.
