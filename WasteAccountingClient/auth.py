from PyQt6.QtWidgets import QDialog, QVBoxLayout, QLabel, QLineEdit, QPushButton, QMessageBox
from PyQt6.QtCore import Qt
from api_client import APIClient


class LoginDialog(QDialog):
    def __init__(self):
        super().__init__()
        self.client = APIClient()
        self.setWindowTitle("Авторизация")
        self.setFixedSize(420, 320)
        self.setWindowFlags(Qt.WindowType.WindowCloseButtonHint | Qt.WindowType.WindowTitleHint)

        self.setStyleSheet("""
            QDialog {
                background-color: #f5f7fa;
            }
            QLabel {
                color: #212529;
            }
            QLineEdit {
                background-color: #ffffff;
                border: 1px solid #ced4da;
                border-radius: 8px;
                padding: 12px 14px;
                font-size: 14px;
                min-height: 20px;
            }
            QLineEdit:focus {
                border-color: #0066cc;
            }
            QPushButton {
                background-color: #0066cc;
                color: white;
                border: none;
                padding: 12px;
                border-radius: 8px;
                font-size: 14px;
                font-weight: 500;
                min-height: 20px;
            }
            QPushButton:hover {
                background-color: #0052a3;
            }
        """)

        layout = QVBoxLayout()
        layout.setSpacing(20)
        layout.setContentsMargins(35, 35, 35, 35)

        title = QLabel("Модуль учета и классификации отходов")
        title.setAlignment(Qt.AlignmentFlag.AlignCenter)
        title.setStyleSheet("font-size: 16px; font-weight: bold; color: #212529; margin-bottom: 10px;")
        layout.addWidget(title)

        # Email
        self.email_input = QLineEdit()
        self.email_input.setPlaceholderText("Email")
        self.email_input.setMinimumHeight(40)
        layout.addWidget(self.email_input)

        # Пароль
        self.password_input = QLineEdit()
        self.password_input.setPlaceholderText("Пароль")
        self.password_input.setEchoMode(QLineEdit.EchoMode.Password)
        self.password_input.setMinimumHeight(40)
        layout.addWidget(self.password_input)

        # Кнопка входа
        self.login_btn = QPushButton("Войти")
        self.login_btn.setMinimumHeight(45)
        self.login_btn.clicked.connect(self.authenticate)
        layout.addWidget(self.login_btn)

        # Подсказка
        hint = QLabel("operator@example.com / 12345\nchief@example.com / 12345")
        hint.setAlignment(Qt.AlignmentFlag.AlignCenter)
        hint.setStyleSheet("color: #888888; font-size: 10px; margin-top: 10px;")
        layout.addWidget(hint)

        layout.addStretch()
        self.setLayout(layout)

    def authenticate(self):
        email = self.email_input.text().strip()
        password = self.password_input.text().strip()

        if not email or not password:
            QMessageBox.warning(self, "Ошибка", "Введите email и пароль")
            return

        result = self.client.login(email, password)

        if result:
            role = result.get("role", "operator")
            full_name = result.get("full_name", email.split('@')[0])

            from main_window import MainWindow
            self.accept()
            self.main_window = MainWindow({
                "login": email,
                "full_name": full_name,
                "role": role
            })
            self.main_window.show()
        else:
            QMessageBox.warning(self, "Ошибка", "Неверный email или пароль")