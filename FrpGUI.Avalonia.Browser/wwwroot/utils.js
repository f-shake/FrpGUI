﻿export function setLocalStorage(key, value) {
    localStorage.setItem(key, value);
}

export function getLocalStorage(key) {
    return localStorage.getItem(key);
}

export function showAlert(message) {
    alert(message);
}

export function reload() {
    location.reload(true)
}

export function getCurrentUrl() {
    return window.location.href
}
export function openUrl() {
    return window.open("url", "_blank")
}