function run(desiredSkillJson) {
    var forbiddenSolutions = [];
    var prob = new MHWProblem();

    var desiredSkills = JSON.parse(desiredSkillJson);
    for (var j = 0; j < desiredSkills.length; j++) {
        prob.requireSkill(desiredSkills[j][0], desiredSkills[j][1]);
    }

    var iteCount = 10;
    var solutions = [];
    var forbiddenSolutions = [];

    for (var i = 0; i < iteCount; ++i) {
        prob.solve(forbiddenSolutions);

        if (prob.solution.solved) {
            solutions.push(prob.formatStats() + '\n');
        } else {
            continue;
        }

        forbiddenSolutions.push(
            Object
            .getOwnPropertyNames(prob.solution.variables)
            .filter(varId => prob.solution.variables[varId] && (varId[0] === 'a' || varId[0] === 'c'))
        );
    }

    return JSON.stringify(solutions);
}