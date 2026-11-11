# Acer Crate

**Acer Crate** is a lightweight, independent open-source alternative to **Acer NitroSense** for Windows. Based on an OpenSense fork, this utility is specifically designed and optimized for **Acer Nitro 5 (AN515-58)** gaming laptops, offering full control over cooling, performance, and lighting without bloatware.

---

## ✨ Key Features

* **Fan Control:** Manual CPU/GPU fan speed adjustment, automatic curves, and maximum cooling mode.
* **Performance Modes:** Quick switching between Quiet, Balanced, and Performance power profiles.
* **Keyboard Lighting:** Customize RGB keyboard colors, lighting effects, and brightness levels.
* **Custom Presets:** Create, save, and switch between your own cooling and lighting profiles for gaming or work.
* **Multilingual Interface:** Native UI support for English, Ukrainian, and Russian.
* **Lightweight & Clean:** Zero telemetry, no heavy background services, and minimal RAM/CPU usage compared to official software.

---

## 💻 System Requirements & Compatibility

* **Target Laptop:** Acer Nitro 5 **AN515-58** *(other Acer Nitro models may work, but are untested)*
* **OS:** Windows 10 / Windows 11 (64-bit)
* **Permissions:** Administrator privileges are required to access hardware sensors and the Embedded Controller (EC).

---

## 🚀 Installation & Usage

1. Go to the **[Releases](https://github.com)** page and download the latest build archive.
2. Extract the files to a permanent folder (e.g., `C:\Program Files\Acer Crate`).
3. **Uninstall the official Acer NitroSense** to prevent conflicts when accessing the fan controller.
4. Run **`Acer Crate.exe`** **as Administrator**.

---

## 🛠️ Building from Source

If you prefer to compile the application yourself:

1. Clone the repository:
   ```bash
   git clone https://github.com
   ```
2. Open the project in **Visual Studio** (with the `.NET Desktop Development` workload installed).
3. Switch the build configuration to **Release** and build the solution.

---

## ⚠️ Disclaimer

This software communicates directly with your laptop's Embedded Controller (EC) to manage hardware sensors and fan speeds. Use it at your own risk. The author is not responsible for any potential hardware issues or warranty claims.

---

## 📄 License

This project is open-source. Check the repository files for license details.
<img width="1280" height="708" alt="image" src="https://github.com/user-attachments/assets/00343473-d0ba-4c6b-bdf2-210b9b0de347" />
