# Good Burger, Great Burger — User Manual

A cooperative multiplayer restaurant cooking game built with Unity and Photon Fusion. Players log in, create or join a room, then work together to prepare and serve customer orders before time runs out.

This guide is written for reviewers and demo evaluators running the project in the **Unity Editor**.

---

## Requirements

| Item | Version / note |
|------|----------------|
| Unity Editor | **6000.3.10f1** (install via [Unity Hub](https://unity.com/download)) |
| OS | Windows PC (project target) |
| Internet | Required for account API and Photon multiplayer |

No separate player build is required for evaluation. Open the project in Unity and press **Play**.

---

## Installation

1. Clone this repository.
2. Open **Unity Hub** → **Add** → select the project folder.
3. When prompted, use Unity **6000.3.10f1**.
4. Wait for the Editor to import assets (first open may take several minutes).
5. Open the scene **`Assets/3. Scenes/MinJun/Title.unity`**.
6. Press **Play** in the Editor.

> **Start scene:** Always begin from **Title**. Other scenes expect prior login and room state.

---

## Server Configuration

The game uses a REST API for accounts and room metadata. Multiplayer gameplay uses **Photon Fusion** (configured in the project).

### Option A — Hosted server (default, recommended)

The **Title** scene is preconfigured to use the cloud server:

- URL: `https://mse-server.onrender.com`
- `NetworkManager` → **Use Local** is **unchecked** (`useLocal = false`)

**Cold-start delay:** Render free-tier instances sleep when idle. The **first login or API call after a long idle period may take 30–60+ seconds**. Wait and try again if Register/Login appears to hang or times out.

### Option B — Local server (alternative)

Use this if the hosted server is unavailable or you need offline API testing.

1. Clone and run the backend: [MSE_Server](https://github.com/lmj0927/MSE_Server)
   ```bash
   # Windows
   mvnw.cmd spring-boot:run
   ```
   Default base URL: `http://localhost:9090` (see server [README](https://github.com/lmj0927/MSE_Server))

2. In Unity, open **`Assets/3. Scenes/MinJun/Title.unity`**.
3. Select the **NetworkManager** object in the Hierarchy.
4. In the Inspector:
   - Check **Use Local**
   - Set **Base Url** to `http://localhost:9090`

API details: [`Docs/API.md`](Docs/API.md)

---

## Account Registration & Login

There is **no pre-made test account**. Create your own on the Title screen.

### Credentials (server rules)

| Field | Rules |
|-------|--------|
| **User ID** | 3–64 characters, no spaces |
| **Password** | 8–128 characters, no spaces |

**Example (valid format):**

- User ID: `test1234`
- Password: `test1234`

### Steps

1. On the **Title** screen, click **Register** and enter a new user ID and password.
2. Click **Login** with the same credentials.
3. On success, the game loads the **JoinRoom** scene.

If login fails on the hosted server, wait for the Render instance to wake up and retry.

---

## Game Flow

Build order (see **File → Build Profiles → Scenes**):

```
Title → JoinRoom → InRoom → Multi Main Test → Stage scenes → back to JoinRoom
```

| Step | Scene | What to do |
|------|-------|------------|
| 1 | **Title** | Register → Login |
| 2 | **JoinRoom** | **Create** a room *or* join an existing room from the list |
| 3 | **InRoom** | Wait in the lobby; the **host** clicks **Start** |
| 4 | **Multi Main Test** | Photon session starts; players spawn in the restaurant |
| 5 | **Stage 1 / Stage 2** | Play the selected stage |
| 6 | End of stage | Returns to **JoinRoom** after a short delay |

### Creating a room (JoinRoom)

1. Click **Create**.
2. Enter a **room title** (1–128 characters).
3. Select a **stage** from the dropdown (Stage 1 is available for new accounts).
4. Set **max players** to **2, 3, or 4**.
5. Click **Create**.

You are taken to **InRoom** automatically. **Solo play is supported** — set max players to 2 (or higher) and start alone as host.

### Starting the game (InRoom)

- Only the **room host** can press **Start**.
- All connected players enter the multiplayer restaurant together.

---

## Controls

| Action | Key |
|--------|-----|
| Move | **W A S D** |
| Run | **Left Shift** (uses stamina) |
| Interact (pick up, place, use stations) | **E** |
| Close refrigerator ingredient menu | **Esc** |

All kitchen stations are used through **E** while standing near them.

---

## How to Play

### Objective

Read the **order list in the top-left** of the screen. It shows each customer’s menu and required ingredients. Prepare the correct dishes and deliver them to customers to earn score and stars.

### Basic loop

1. **Refrigerator** — Press **E** to open the ingredient menu and take raw items.
2. **Prepare ingredients** at the correct station (slice, grill, fry, assemble — see below).
3. **Tray** — Place the main dish, side, and drink on a serving tray as needed.
4. **Serve** — Deliver the completed order to the customer.
5. Repeat until the stage timer ends.

### Kitchen stations

| Station | Recipe type | How it works |
|---------|-------------|--------------|
| **Refrigerator** | — | **E** → select ingredient from UI |
| **Slice counter** | Slice | Place item, press **E** repeatedly until slicing completes |
| **Grill** | Grill | Place item, wait for cook timer, **E** to pick up when done |
| **Fryer** | Oil | Place item, wait for fry timer, **E** to pick up when done |
| **Cooking / assembly counter** | Assemble | Place all burger parts on the counter, **E** to combine into one burger |
| **Drink counter** | Beverage | **E** to start; press **E** again in the timing window to fill the cup |
| **Tray** | — | Place main, side, and drink on one tray for delivery |
| **Trash** | — | Discard unwanted items |

> **Side dishes:** After frying, picking up **French fries**, **chicken leg**, or **fish fries** automatically packages them into the finished side item.

---

## Ingredient Preparation Reference

Data lives under `Assets/4. Data/SO/Recipes/` and `Assets/4. Data/SO/Foods/`.

### Slice counter (press **E** per slice)

| Input | Output | Slices |
|-------|--------|--------|
| Lettuce | Sliced Lettuce | 6 |
| Tomato | Sliced Tomato | 4 |
| Onion | Sliced Onion | 4 |
| Mushroom | Sliced Mushroom | 3 |
| Potato | Raw French Fries | 5 |
| Cod | Cod Fillet | 6 |
| Lobster | Raw Lobster Patty | 6 |

### Grill (wait, then **E** to collect)

| Input | Output | Cook time (sec) |
|-------|--------|-----------------|
| Raw Beef | Beef Patty | 6 |
| Onion | Baked Onion | 3 |
| Mushroom | Baked Mushroom | 4 |

### Fryer (wait, then **E** to collect)

| Input | Output | Fry time (sec) |
|-------|--------|----------------|
| Raw French Fries | Fried French Fries → **French Fries** (when picked up) | 2 |
| Raw Lobster Patty | Lobster Patty | 2 |
| Raw Chicken Leg | Cooked Chicken Leg → **Chicken Leg** (when picked up) | 3 |
| Cod Fillet | Cooked Fish Fries → **Fish Fries** (when picked up) | 3 |

### Burgers (assembly counter)

Place every listed ingredient on the counter, then press **E** to assemble.

| Burger | Ingredients |
|--------|-------------|
| **Basic Burger** | Bread, Beef, Sliced Lettuce |
| **Cheese Burger** | Bread, Beef, Cheese |
| **Regular Burger** | Bread, Beef, Sliced Lettuce, Sliced Tomato, Cheese |
| **Double Burger** | Bread ×2, Beef ×2, Cheese, Sliced Lettuce |
| **Triple Burger** | Bread ×2, Beef ×3, Cheese, Sliced Lettuce, Sliced Tomato |
| **Signature Burger** | Bread, Beef, Sliced Lettuce, Sliced Tomato, Lobster Patty |
| **Supreme Burger** | Bread, Lobster Patty, Sliced Lettuce, Baked Onion, Cheese |
| **Lobster Burger** | Bread, Lobster Patty, Sliced Lettuce, Sliced Onion |
| **Grilled Mushroom Burger** | Bread, Beef, Baked Onion, Baked Mushroom |

### Beverage

| Item | Station |
|------|---------|
| **Coke** | Drink counter — timing minigame (**E** in the green zone) |

---

## Testing Cheats (Editor Only)

For quick stage-completion checks during Play mode:

1. Enter a stage as the **host** (state authority).
2. In the Hierarchy, select the **GameManager** object.
3. In the Inspector, under **Debug**, click **Force End Stage (1★ Score + Save Progress)**.

This immediately ends the stage with a 1-star clear score and saves progress. Usable only while the game is in **Play** mode and the host is in an active playing state.

---

## Troubleshooting

| Issue | Suggestion |
|-------|------------|
| Login/register very slow or fails | Hosted server may be waking from sleep; wait up to ~1 minute and retry |
| “You are not logged in” | Return to **Title** and log in again |
| Cannot start game | Only the **host** can press **Start** in InRoom |
| Photon connection fails | Check internet; ensure project Photon/Fusion settings are intact |
| Local API errors | Confirm server is running on port **9090** and **Use Local** is enabled on NetworkManager |

---

## Related Documentation

- [MSE_Server](https://github.com/lmj0927/MSE_Server) — Backend repository
