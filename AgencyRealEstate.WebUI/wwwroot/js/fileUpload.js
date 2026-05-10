window.uploadPhotosViaJS = async (inputElement, propertyId) => {
    const files = inputElement.files;
    if (!files || files.length === 0) {
        return { success: false, message: 'Файлы не выбраны' };
    }

    const formData = new FormData();
    for (let i = 0; i < files.length; i++) {
        formData.append('files', files[i]);
    }

    try {
        const response = await fetch(`https://localhost:7159/api/images/upload/${propertyId}`, {
            method: 'POST',
            body: formData,
            credentials: 'include'
        });

        if (response.ok) {
            return { success: true };
        } else {
            const errorText = await response.text();
            console.error("Сервер вернул ошибку:", response.status, errorText);
            return { success: false, message: `Ошибка ${response.status}: ${errorText}` };
        }
    } catch (error) {
        console.error("Ошибка при выполнении fetch:", error);
        return { success: false, message: error.message };
    }
};