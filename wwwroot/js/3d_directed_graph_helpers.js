function zoom_to_node(node) {
    // Aim at node from outside it
    const distance = 40;
    const distRatio = 1 + distance / Math.hypot(node.x, node.y, node.z);

    const newPos = node.x || node.y || node.z
        ? {x: node.x * distRatio, y: node.y * distRatio, z: node.z * distRatio}
        : {x: 0, y: 0, z: distance}; // special case if node is in (0,0,0)

    Graph.cameraPosition(
        newPos, // new position
        node, // lookAt ({ x, y, z })
        3000  // ms transition duration
    );
}
window.zoom_to_node = zoom_to_node;