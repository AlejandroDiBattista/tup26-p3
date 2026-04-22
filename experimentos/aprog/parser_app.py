import json
from dataclasses import dataclass

class ParserError(Exception):
    pass

@dataclass
class NumberNode:
    value: float

@dataclass
class BinaryOpNode:
    op: str
    left: object
    right: object

@dataclass
class UnaryOpNode:
    op: str
    operand: object

class Lexer:
    def __init__(self, text: str):
        self.text = text
        self.pos = 0

    def current_char(self):
        if self.pos >= len(self.text):
            return None
        return self.text[self.pos]

    def advance(self):
        self.pos += 1

    def skip_whitespace(self):
        while self.current_char() is not None and self.current_char().isspace():
            self.advance()

    def number(self):
        start = self.pos
        has_dot = False

        while self.current_char() is not None:
            ch = self.current_char()
            if ch.isdigit():
                self.advance()
            elif ch == "." and not has_dot:
                has_dot = True
                self.advance()
            else:
                break

        value = self.text[start:self.pos]
        if value == ".":
            raise ParserError("Número inválido")
        return ("NUMBER", float(value))

    def tokens(self):
        result = []

        while self.current_char() is not None:
            ch = self.current_char()

            if ch.isspace():
                self.skip_whitespace()
            elif ch.isdigit() or ch == ".":
                result.append(self.number())
            elif ch in "+-*/()":
                result.append((ch, ch))
                self.advance()
            else:
                raise ParserError(f"Carácter inesperado: {ch}")

        result.append(("EOF", None))
        return result


class Parser:
    def __init__(self, text: str):
        self.tokens = Lexer(text).tokens()
        self.pos = 0
        self.current = self.tokens[self.pos]

    def eat(self, token_type):
        if self.current[0] == token_type:
            self.pos += 1
            self.current = self.tokens[self.pos]
        else:
            raise ParserError(f"Se esperaba {token_type} y se encontró {self.current[0]}")

    def parse(self):
        node = self.expr()
        if self.current[0] != "EOF":
            raise ParserError("Expresión inválida")
        return node

    def expr(self):
        node = self.term()

        while self.current[0] in ("+", "-"):
            op = self.current[0]
            self.eat(op)
            node = BinaryOpNode(op=op, left=node, right=self.term())

        return node

    def term(self):
        node = self.factor()

        while self.current[0] in ("*", "/"):
            op = self.current[0]
            self.eat(op)
            node = BinaryOpNode(op=op, left=node, right=self.factor())

        return node

    def factor(self):
        token_type, token_value = self.current

        if token_type == "+":
            self.eat("+")
            return UnaryOpNode(op="+", operand=self.factor())

        if token_type == "-":
            self.eat("-")
            return UnaryOpNode(op="-", operand=self.factor())

        if token_type == "NUMBER":
            self.eat("NUMBER")
            return NumberNode(token_value)

        if token_type == "(":
            self.eat("(")
            node = self.expr()
            self.eat(")")
            return node

        raise ParserError(f"Token inesperado: {token_type}")

class Evaluator:
    def visit(self, node):
        if isinstance(node, NumberNode):
            return node.value

        if isinstance(node, UnaryOpNode):
            value = self.visit(node.operand)
            return value if node.op == "+" else -value

        if isinstance(node, BinaryOpNode):
            left = self.visit(node.left)
            right = self.visit(node.right)

            if node.op == "+":
                return left + right
            if node.op == "-":
                return left - right
            if node.op == "*":
                return left * right
            if node.op == "/":
                if right == 0:
                    raise ParserError("División por cero")
                return left / right

        raise ParserError("Nodo AST inválido")

def node_to_dict(node):
    if isinstance(node, NumberNode):
        return {"type": "Number", "value": node.value}

    if isinstance(node, UnaryOpNode):
        return {
            "type": "UnaryOp",
            "op": node.op,
            "operand": node_to_dict(node.operand),
        }

    if isinstance(node, BinaryOpNode):
        return {
            "type": "BinaryOp",
            "op": node.op,
            "left": node_to_dict(node.left),
            "right": node_to_dict(node.right),
        }

    raise ParserError("Nodo AST inválido")

def ast_to_json(node):
    return json.dumps(node_to_dict(node), indent=2, ensure_ascii=False)

def format_ast_tree(node, prefix="", is_last=True):
    connector = "└── " if is_last else "├── "

    if isinstance(node, NumberNode):
        label = f"Number({node.value})"
        return f"{prefix}{connector}{label}"

    if isinstance(node, UnaryOpNode):
        label = f"UnaryOp({node.op})"
        next_prefix = prefix + ("    " if is_last else "│   ")
        child = format_ast_tree(node.operand, next_prefix, True)
        return f"{prefix}{connector}{label}\n{child}"

    if isinstance(node, BinaryOpNode):
        label = f"BinaryOp({node.op})"
        next_prefix = prefix + ("    " if is_last else "│   ")
        left = format_ast_tree(node.left, next_prefix, False)
        right = format_ast_tree(node.right, next_prefix, True)
        return f"{prefix}{connector}{label}\n{left}\n{right}"

    return f"{prefix}{connector}NodoDesconocido"


def build_ast(expression: str):
    return Parser(expression).parse()

def evaluate(expression: str) -> float:
    ast = build_ast(expression)
    return Evaluator().visit(ast)

def main():
    print("Analizador recursivo descendente con AST")
    print("Ingresá una expresión o escribí 'salir'.")

    while True:
        expr = input("> ").strip()

        if expr.lower() in {"salir", "exit", "quit"}:
            break

        if not expr:
            continue

        try:
            ast = build_ast(expr)
            result = Evaluator().visit(ast)
            if result.is_integer():
                result = int(result)

            print("AST como árbol:")
            print(format_ast_tree(ast))
            print("AST como JSON:")
            print(ast_to_json(ast))
            print(f"Resultado: {result}")
        except ParserError as e:
            print(f"Error: {e}")


if __name__ == "__main__":
    main()
