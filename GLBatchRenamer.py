import os
import platform
import tkinter as tk
from tkinter import ttk, filedialog
import webbrowser

# ------------------ Core Functions ------------------ #
def search_and_replace(directory, search_replace):
    for filename in os.listdir(directory):
        if os.path.isfile(os.path.join(directory, filename)):
            new_filename = filename.replace(search_replace[0], search_replace[1])
            os.rename(os.path.join(directory, filename), os.path.join(directory, new_filename))

def add_prefix(directory, prefix):
    for filename in os.listdir(directory):
        if os.path.isfile(os.path.join(directory, filename)):
            new_filename = f"{prefix}{filename}"
            os.rename(os.path.join(directory, filename), os.path.join(directory, new_filename))

def add_suffix(directory, suffix):
    for filename in os.listdir(directory):
        if os.path.isfile(os.path.join(directory, filename)):
            name, extension = os.path.splitext(filename)
            new_filename = f"{name}{suffix}{extension}"
            os.rename(os.path.join(directory, filename), os.path.join(directory, new_filename))

# ------------------ UI Functions ------------------ #
def browse_directory():
    directory_path = filedialog.askdirectory()
    directory_var.set(directory_path)

def search_and_replace_ui():
    search_and_replace(directory_var.get(), (search_var.get(), replace_var.get()))

def add_prefix_ui():
    add_prefix(directory_var.get(), prefix_var.get())

def add_suffix_ui():
    add_suffix(directory_var.get(), suffix_var.get())

def open_github(event):
    webbrowser.open("https://github.com/Jappomo")

def toggle_theme():
    global dark_mode
    dark_mode = not dark_mode
    apply_theme()

def apply_theme():
    bg_color = "#2e2e2e" if dark_mode else "#ffffff"
    fg_color = "#ffffff" if dark_mode else "#000000"
    entry_bg = "#3c3c3c" if dark_mode else "#f0f0f0"

    root.configure(bg=bg_color)
    for widget in root.winfo_children():
        if isinstance(widget, tk.Entry):
            widget.configure(bg=entry_bg, fg=fg_color, relief="flat", highlightthickness=1, highlightbackground="#666")
        elif isinstance(widget, ttk.Label):
            widget.configure(background=bg_color, foreground=fg_color)
        elif isinstance(widget, tk.Label):
            widget.configure(background=bg_color, foreground=fg_color)
        elif isinstance(widget, tk.Button):
            widget.configure(bg="#4a90e2", fg="white", relief="flat", bd=0, padx=10, pady=5, highlightthickness=0)

# ------------------ Main Window ------------------ #
root = tk.Tk()
root.title("File Batch Renamer")
root.geometry("520x360")
root.resizable(False, False)

# Variables
directory_var = tk.StringVar()
search_var = tk.StringVar()
replace_var = tk.StringVar()
prefix_var = tk.StringVar()
suffix_var = tk.StringVar()
dark_mode = False

# Widgets
ttk.Label(root, text="Directory:").grid(row=0, column=0, sticky="w", pady=10, padx=10)
dir_entry = tk.Entry(root, textvariable=directory_var, width=40, relief="flat", bd=0)
dir_entry.grid(row=0, column=1, columnspan=2, pady=10)
tk.Button(root, text="Browse", command=browse_directory).grid(row=0, column=3, padx=5)

ttk.Label(root, text="Search:").grid(row=1, column=0, pady=5, padx=10, sticky="w")
tk.Entry(root, textvariable=search_var, relief="flat", bd=0).grid(row=1, column=1, columnspan=2, pady=5)

ttk.Label(root, text="Replace:").grid(row=2, column=0, pady=5, padx=10, sticky="w")
tk.Entry(root, textvariable=replace_var, relief="flat", bd=0).grid(row=2, column=1, columnspan=2, pady=5)

tk.Button(root, text="Search & Replace", command=search_and_replace_ui).grid(row=3, column=1, columnspan=2, pady=10)

ttk.Label(root, text="Prefix:").grid(row=4, column=0, pady=5, padx=10, sticky="w")
tk.Entry(root, textvariable=prefix_var, relief="flat", bd=0).grid(row=4, column=1, columnspan=2, pady=5)
tk.Button(root, text="Add Prefix", command=add_prefix_ui).grid(row=4, column=3, padx=5)

ttk.Label(root, text="Suffix:").grid(row=5, column=0, pady=5, padx=10, sticky="w")
tk.Entry(root, textvariable=suffix_var, relief="flat", bd=0).grid(row=5, column=1, columnspan=2, pady=5)
tk.Button(root, text="Add Suffix", command=add_suffix_ui).grid(row=5, column=3, padx=5)

# Dark/Light Mode Switch
theme_switch = tk.Button(root, text="🌙 Toggle Dark Mode", command=toggle_theme)
theme_switch.grid(row=6, column=1, columnspan=2, pady=15)

# Developed by + Version
dev_label = tk.Label(root, text="Developed by Jappomo", fg="blue", cursor="hand2")
dev_label.grid(row=7, column=0, columnspan=4, pady=(10, 0))
dev_label.bind("<Button-1>", open_github)

version_label = tk.Label(root, text="Version 1.1v", fg="gray")
version_label.grid(row=8, column=0, columnspan=4, pady=(2, 5))

# Apply initial theme
apply_theme()

root.mainloop()
