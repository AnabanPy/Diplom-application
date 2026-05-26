from PyQt6.QtWidgets import (QWidget, QVBoxLayout, QFormLayout, QComboBox,
                             QLineEdit, QPushButton, QMessageBox, QGroupBox,
                             QHBoxLayout, QLabel, QListWidget, QScrollArea)
from PyQt6.QtCore import Qt
from api_client import APIClient


class RegisterTab(QWidget):
    def __init__(self, user):
        super().__init__()
        self.user = user
        self.client = APIClient()
        self.waste_types = []
        self.filtered_types = []
        self.selected_waste = None
        self.existing_codes = []
        self.init_ui()
        self.load_waste_types()
        self.load_existing_codes()

    def init_ui(self):
        scroll = QScrollArea()
        scroll.setWidgetResizable(True)
        scroll.setStyleSheet("QScrollArea { border: none; background-color: transparent; }")

        container = QWidget()
        layout = QVBoxLayout(container)
        layout.setSpacing(20)
        layout.setContentsMargins(30, 30, 30, 30)

        title = QLabel("Регистрация новой партии отходов")
        title.setObjectName("title")
        layout.addWidget(title)

        group = QGroupBox("Данные партии")
        form_layout = QFormLayout()
        form_layout.setSpacing(16)

        # Код партии
        self.code_input = QLineEdit()
        self.code_input.setReadOnly(True)
        self.code_input.setMinimumHeight(35)
        form_layout.addRow("Код партии:", self.code_input)

        # Поиск отхода
        self.search_input = QLineEdit()
        self.search_input.setPlaceholderText("🔍 Поиск по справочнику ФККО (код или название)...")
        self.search_input.setMinimumHeight(35)
        self.search_input.textChanged.connect(self.filter_waste_list)
        form_layout.addRow("Быстрый поиск:", self.search_input)

        # Список отходов
        self.waste_list = QListWidget()
        self.waste_list.setMaximumHeight(250)
        self.waste_list.setMinimumHeight(150)
        self.waste_list.itemClicked.connect(self.on_waste_selected)
        form_layout.addRow("Доступные виды отходов (ФККО):", self.waste_list)

        # Наименование отхода
        self.name_input = QLineEdit()
        self.name_input.setPlaceholderText("Заполнится автоматически")
        self.name_input.setReadOnly(True)
        self.name_input.setMinimumHeight(35)
        form_layout.addRow("Наименование отхода:", self.name_input)

        # Код ФККО
        self.fkko_input = QLineEdit()
        self.fkko_input.setPlaceholderText("Заполнится автоматически")
        self.fkko_input.setReadOnly(True)
        self.fkko_input.setMinimumHeight(35)
        form_layout.addRow("Код ФККО:", self.fkko_input)

        # Класс опасности
        self.hazard_combo = QComboBox()
        self.hazard_combo.addItems([
            "1 (чрезвычайно опасный)",
            "2 (высокоопасный)",
            "3 (умеренно опасный)",
            "4 (малоопасный)",
            "5 (практически неопасный)"
        ])
        self.hazard_combo.setMinimumHeight(35)
        form_layout.addRow("Класс опасности:", self.hazard_combo)

        # Объем
        volume_layout = QHBoxLayout()
        self.volume_input = QLineEdit()
        self.volume_input.setPlaceholderText("0.00")
        self.volume_input.setMinimumHeight(35)
        self.volume_input.setMinimumWidth(150)
        volume_layout.addWidget(self.volume_input)

        self.unit_combo = QComboBox()
        self.unit_combo.addItems(["тонн", "м³", "кг", "л"])
        self.unit_combo.setMinimumHeight(35)
        self.unit_combo.setMinimumWidth(100)
        volume_layout.addWidget(self.unit_combo)
        volume_layout.addStretch()
        form_layout.addRow("Объем:", volume_layout)

        # Срок хранения
        self.deadline_input = QLineEdit()
        self.deadline_input.setPlaceholderText("48")
        self.deadline_input.setText("48")
        self.deadline_input.setMinimumHeight(35)
        form_layout.addRow("Срок хранения (часов):", self.deadline_input)

        # Цех-источник
        self.source_input = QLineEdit()
        self.source_input.setPlaceholderText("Цех-источник")
        self.source_input.setMinimumHeight(35)
        form_layout.addRow("Цех-источник:", self.source_input)

        group.setLayout(form_layout)
        layout.addWidget(group)

        # Кнопки
        btn_layout = QHBoxLayout()
        self.save_btn = QPushButton("💾 Зарегистрировать партию")
        self.save_btn.setMinimumHeight(45)
        self.save_btn.setStyleSheet("background-color: #27ae60; font-weight: bold; font-size: 14px;")
        self.save_btn.clicked.connect(self.save_batch)

        self.clear_btn = QPushButton("🗑 Очистить")
        self.clear_btn.setObjectName("secondary")
        self.clear_btn.setMinimumHeight(45)
        self.clear_btn.clicked.connect(self.clear_form)

        btn_layout.addWidget(self.save_btn)
        btn_layout.addWidget(self.clear_btn)
        layout.addLayout(btn_layout)

        layout.addStretch()

        scroll.setWidget(container)
        main_layout = QVBoxLayout(self)
        main_layout.setContentsMargins(0, 0, 0, 0)
        main_layout.addWidget(scroll)
        self.setLayout(main_layout)

    def load_waste_types(self):
        """Загрузка справочника ФККО с сервера"""
        try:
            print("Загрузка справочника ФККО с сервера...")
            self.waste_types = self.client.get_waste_types()
            print(f"Загружено {len(self.waste_types)} видов отходов из ФККО")
        except Exception as e:
            print(f"Ошибка загрузки ФККО: {e}")
            self.waste_types = []

        self.filtered_types = self.waste_types.copy()
        self.update_waste_list()

    def update_waste_list(self):
        self.waste_list.clear()
        if not self.filtered_types:
            self.waste_list.addItem("❌ Нет данных. Проверьте подключение к серверу.")
            return

        for wt in self.filtered_types:
            hazard_text = {1: "I", 2: "II", 3: "III", 4: "IV", 5: "V"}.get(wt.get('hazard_class', 4),
                                                                           str(wt.get('hazard_class', 4)))
            item_text = f"{wt.get('code', '???')} — {wt.get('name', '?')} [ФККО: {wt.get('fkko_code', '—')}] [Кл.{hazard_text}]"
            self.waste_list.addItem(item_text)

    def filter_waste_list(self):
        query = self.search_input.text().strip().lower()
        if not query:
            self.filtered_types = self.waste_types.copy()
        else:
            self.filtered_types = [
                wt for wt in self.waste_types
                if query in wt.get('name', '').lower()
                   or query in wt.get('code', '').lower()
                   or query in wt.get('fkko_code', '').lower()
            ]
        self.update_waste_list()

    def on_waste_selected(self, item):
        row = self.waste_list.row(item)
        if 0 <= row < len(self.filtered_types):
            self.selected_waste = self.filtered_types[row]
            self.name_input.setText(self.selected_waste.get('name', ''))
            self.fkko_input.setText(self.selected_waste.get('fkko_code', ''))
            hazard = self.selected_waste.get('hazard_class', 4)
            self.hazard_combo.setCurrentIndex(hazard - 1)

    def load_existing_codes(self):
        try:
            batches = self.client.get_batches()
            self.existing_codes = [b.get('code') for b in batches if b.get('code')]
            self.generate_next_code()
        except Exception as e:
            print(f"Ошибка загрузки кодов: {e}")
            self.code_input.setText("P1")

    def generate_next_code(self):
        if not self.existing_codes:
            new_code = "P1"
        else:
            numbers = []
            for code in self.existing_codes:
                if code and code.startswith('P'):
                    try:
                        num = int(code[1:])
                        numbers.append(num)
                    except:
                        pass
            if numbers:
                next_num = max(numbers) + 1
                new_code = f"P{next_num}"
            else:
                new_code = "P1"
        self.code_input.setText(new_code)

    def save_batch(self):
        if not self.selected_waste:
            QMessageBox.warning(self, "Ошибка", "Выберите вид отхода из списка ФККО")
            return

        try:
            volume = float(self.volume_input.text()) if self.volume_input.text() else 0
            if volume <= 0:
                QMessageBox.warning(self, "Ошибка", "Введите положительный объем")
                return
        except ValueError:
            QMessageBox.warning(self, "Ошибка", "Неверный формат объема")
            return

        try:
            deadline = float(self.deadline_input.text()) if self.deadline_input.text() else 48
            if deadline <= 0:
                deadline = 48
        except ValueError:
            deadline = 48

        unit = self.unit_combo.currentText()
        volume_tons = volume
        if unit in ["кг", "л"]:
            volume_tons = volume / 1000
        elif unit == "м³":
            volume_tons = volume * 1.5

        data = {
            "code": self.code_input.text().strip(),
            "name": self.name_input.text().strip(),
            "fkko_code": self.fkko_input.text().strip(),
            "hazard_class": self.hazard_combo.currentIndex() + 1,
            "volume_tons": round(volume_tons, 3),
            "storage_deadline_hours": deadline,
        }

        if self.source_input.text().strip():
            data["source_department"] = self.source_input.text().strip()

        try:
            response = self.client.create_batch(data)
            QMessageBox.information(self, "Успех",
                                    f"Партия зарегистрирована!\nКод: {response.get('code')}\nОбъем: {volume} {unit}")
            self.existing_codes.append(response.get('code'))
            self.generate_next_code()
            self.clear_form()

            main_window = self.window()
            if hasattr(main_window, 'tabs'):
                for i in range(main_window.tabs.count()):
                    tab = main_window.tabs.widget(i)
                    if tab is not None and tab != self:
                        if hasattr(tab, 'load_data'):
                            try:
                                tab.load_data()
                            except:
                                pass
        except Exception as e:
            QMessageBox.critical(self, "Ошибка", f"Не удалось зарегистрировать партию:\n{e}")

    def clear_form(self):
        self.search_input.clear()
        self.name_input.clear()
        self.fkko_input.clear()
        self.volume_input.clear()
        self.unit_combo.setCurrentIndex(0)
        self.deadline_input.setText("48")
        self.source_input.clear()
        self.hazard_combo.setCurrentIndex(3)
        self.selected_waste = None
        self.filtered_types = self.waste_types.copy()
        self.update_waste_list()