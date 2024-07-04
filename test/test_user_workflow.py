import random
import string
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.common.keys import Keys
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.firefox.service import Service as FirefoxService
from webdriver_manager.firefox import GeckoDriverManager
import allure
import pytest
import os

def random_string(length=10):
    letters = string.ascii_letters
    return ''.join(random.choice(letters) for i in range(length))

@allure.step("Register a new user")
def register_user(driver):
    driver.get("http://localhost:3000/register")

    username = random_string()
    password = random_string()

    wait = WebDriverWait(driver, 10)

    # Заполнение первой формы регистрации
    wait.until(EC.presence_of_element_located((By.XPATH, "(//input[@placeholder='Номер телефона вашего ребенка'])[1]"))).send_keys("1234567890")
    wait.until(EC.presence_of_element_located((By.XPATH, "(//input[@placeholder='Придумайте логин'])[1]"))).send_keys(username)
    wait.until(EC.presence_of_element_located((By.XPATH, "(//input[@placeholder='Придумайте пароль'])[1]"))).send_keys(password)
    wait.until(EC.element_to_be_clickable((By.XPATH, "(//button[contains(text(),'Далее')])[1]"))).click()

    # Заполнение второй формы регистрации
    wait.until(EC.presence_of_element_located((By.XPATH, "(//input[@placeholder='Номер телефона родителя'])[1]"))).send_keys("0987654321")
    wait.until(EC.presence_of_element_located((By.XPATH, "(//input[@placeholder='Email родителя'])[1]"))).send_keys(f"{username}@example.com")
    wait.until(EC.element_to_be_clickable((By.XPATH, "(//button[contains(text(),'Создать аккаунт ребенку')])[1]"))).click()

    return username, password

@pytest.fixture
def driver():
    driver = webdriver.Firefox(service=FirefoxService(GeckoDriverManager().install()))
    yield driver
    driver.quit()

def test_register_and_update_profile(driver):
    username, password = register_user(driver)
    allure.attach(driver.get_screenshot_as_png(), name="registration", attachment_type=allure.attachment_type.PNG)

    driver.get("http://localhost:3000")

    print("Открыта главная страница")

    try:
        wait = WebDriverWait(driver, 10)
        
        login_input = wait.until(EC.presence_of_element_located((By.XPATH, "(//input[@placeholder='Логин'])[1]")))
        login_input.send_keys(username)

        password_input = wait.until(EC.presence_of_element_located((By.XPATH, "(//input[@placeholder='Пароль'])[1]")))
        password_input.send_keys(password)

        wait.until(EC.element_to_be_clickable((By.XPATH, "(//button[contains(text(),'Войти')])[1]"))).click()
        allure.attach(driver.get_screenshot_as_png(), name="login", attachment_type=allure.attachment_type.PNG)

        settings_button = wait.until(EC.element_to_be_clickable((By.XPATH, "(//button[@class='header-toggle-button'])[1]")))
        settings_button.click()

        wait.until(EC.element_to_be_clickable((By.XPATH, "(//button[contains(text(),'Настройки')])[1]"))).click()

        nickname_input = wait.until(EC.presence_of_element_located((By.XPATH, "(//input[@placeholder='Введите новый никнейм'])[1]")))
        nickname_input.clear()
        nickname_input.send_keys("Крутой228")

        file_input = wait.until(EC.presence_of_element_located((By.XPATH, "(//input[@type='file'])[1]")))
        file_input.send_keys(os.path.abspath("C:/Smile/test/RIMURI.jpg"))

        wait.until(EC.element_to_be_clickable((By.XPATH, "(//button[contains(text(),'Сохранить')])[1]"))).click()
        allure.attach(driver.get_screenshot_as_png(), name="update_profile", attachment_type=allure.attachment_type.PNG)

        # Закрываем sidebar-overlay, нажав на кнопку "Закрыть"
        wait.until(EC.element_to_be_clickable((By.XPATH, "(//button[contains(text(),'Закрыть')])[1]"))).click()

        search_input = wait.until(EC.presence_of_element_located((By.XPATH, "(//input[@placeholder='Поиск по никнейму или телефону...'])[1]")))
        search_input.send_keys("Крутой228")

        # Пытаемся найти элемент и взаимодействовать с ним
        search_result_item = wait.until(EC.element_to_be_clickable((By.XPATH, "(//span[@class='search-result-nickname'])[1]")))
        search_result_item.click()

        message_input = wait.until(EC.presence_of_element_located((By.XPATH, "(//input[@placeholder='Введите сообщение...'])[1]")))
        message_input.send_keys("Привет!")
        wait.until(EC.element_to_be_clickable((By.XPATH, "(//button[contains(text(),'Отправить')])[1]"))).click()

        allure.attach(driver.get_screenshot_as_png(), name="send_message", attachment_type=allure.attachment_type.PNG)

    except Exception as e:
        print(f"Произошла ошибка: {e}")
        allure.attach(driver.get_screenshot_as_png(), name="error", attachment_type=allure.attachment_type.PNG)
        raise e
