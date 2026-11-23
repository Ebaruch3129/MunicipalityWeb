window.municipalityReportsMap = {
    render: function (reports) {
        var mapElement = document.getElementById("admin-reports-map");
        if (!mapElement || !window.L) return;

        if (mapElement._leafletMap) {
            mapElement._leafletMap.remove();
        }

        var map = L.map(mapElement).setView([31.7683, 35.2137], 8);
        mapElement._leafletMap = map;
        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            maxZoom: 19,
            attribution: "&copy; OpenStreetMap contributors"
        }).addTo(map);

        var bounds = [];
        reports.forEach(function (report) {
            if (report.latitude === null || report.latitude === undefined ||
                report.longitude === null || report.longitude === undefined) return;

            var point = [Number(report.latitude), Number(report.longitude)];
            bounds.push(point);
            var marker = L.marker(point).addTo(map);
            var popup = document.createElement("div");
            popup.className = "map-report-popup";

            var title = document.createElement("strong");
            title.textContent = "פנייה #" + report.id + " · " + report.title;
            popup.appendChild(title);

            var address = document.createElement("p");
            address.textContent = report.categoryName + " · " + report.address;
            popup.appendChild(address);

            var description = document.createElement("p");
            description.textContent = report.description;
            popup.appendChild(description);

            var owner = document.createElement("p");
            owner.textContent = "תושב: " + report.userName + " · " + report.userEmail;
            popup.appendChild(owner);

            var status = document.createElement("span");
            status.className = "badge text-bg-warning";
            status.textContent = report.status === "InProgress" ? "בטיפול" : "התקבלה";
            popup.appendChild(status);

            var mapsLink = document.createElement("a");
            mapsLink.href = "https://www.google.com/maps/search/?api=1&query=" +
                encodeURIComponent(point[0] + "," + point[1]);
            mapsLink.target = "_blank";
            mapsLink.rel = "noopener noreferrer";
            mapsLink.textContent = "פתיחה ב-Google Maps";
            popup.appendChild(mapsLink);

            marker.bindPopup(popup, { maxWidth: 300 });
        });

        if (bounds.length > 0) {
            map.fitBounds(bounds, { padding: [30, 30], maxZoom: 15 });
        }

        window.setTimeout(function () { map.invalidateSize(); }, 100);
    }
};
