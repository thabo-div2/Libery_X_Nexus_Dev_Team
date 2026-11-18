// Saves a file the server sent through Blazor (e.g. a CSV export) to the viewer's downloads.
window.nxDownloadFile = async (fileName, contentType, streamRef) => {
    const buffer = await streamRef.arrayBuffer();
    const blob = new Blob([buffer], { type: contentType || "application/octet-stream" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = fileName || "download";
    document.body.appendChild(link);
    link.click();
    link.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
};
