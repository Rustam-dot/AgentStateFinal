// Theme Toggle Functionality
(function() {
    // Проверяем сохраненную тему при загрузке страницы
    const savedTheme = localStorage.getItem('theme') || 'light';
    document.documentElement.setAttribute('data-theme', savedTheme);

    // Функция переключения темы
    window.toggleTheme = function() {
        const currentTheme = document.documentElement.getAttribute('data-theme');
        const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
        
        document.documentElement.setAttribute('data-theme', newTheme);
        localStorage.setItem('theme', newTheme);
        
        // Обновляем иконку кнопки
        updateThemeIcon(newTheme);
    };

    // Функция обновления иконки
    function updateThemeIcon(theme) {
        const themeIcons = document.querySelectorAll('.theme-toggle-icon');
        themeIcons.forEach(icon => {
            if (theme === 'dark') {
                icon.classList.remove('fa-moon');
                icon.classList.add('fa-sun');
            } else {
                icon.classList.remove('fa-sun');
                icon.classList.add('fa-moon');
            }
        });
    }

    // Инициализация иконки при загрузке
    document.addEventListener('DOMContentLoaded', function() {
        updateThemeIcon(savedTheme);
    });
})();
